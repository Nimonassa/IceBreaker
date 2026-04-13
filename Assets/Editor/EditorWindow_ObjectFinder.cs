using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.ShortcutManagement;
using Object = UnityEngine.Object;

// ==========================================
// OPTIMIZATION: PRE-COMPUTED SEARCH DATA
// ==========================================
public struct SearchData
{
    public string OriginalName;
    public string LowerName;
    public string LowerFullName;
    public string Acronym;
    public List<string> ClassWordsLower;

    public SearchData(string name, string fullName)
    {
        OriginalName = name ?? "";
        LowerName = OriginalName.ToLower();
        LowerFullName = fullName != null ? fullName.ToLower() : LowerName;
        
        Acronym = "";
        foreach (char c in OriginalName) if (char.IsUpper(c)) Acronym += char.ToLower(c);

        ClassWordsLower = new List<string>();
        if (OriginalName.Length > 0)
        {
            int lastIdx = 0;
            for (int i = 1; i < OriginalName.Length; i++)
            {
                if (char.IsUpper(OriginalName[i]) && !char.IsUpper(OriginalName[i - 1]))
                {
                    ClassWordsLower.Add(OriginalName.Substring(lastIdx, i - lastIdx).ToLower());
                    lastIdx = i;
                }
            }
            ClassWordsLower.Add(OriginalName.Substring(lastIdx).ToLower());
        }
    }
}

public struct ComponentCandidate { public Type Type; public SearchData Data; }
public struct StringCandidate { public string Value; public SearchData Data; }
public struct SceneCandidate { public GameObject GameObject; public SearchData Data; }

public struct ScoredItem<T> : IComparable<ScoredItem<T>>
{
    public T Item;
    public int Score;
    public int Penalty; // NEW: Used to deprioritize components with namespaces
    public string NameForSorting;

    public int CompareTo(ScoredItem<T> other)
    {
        int scoreComparison = Score.CompareTo(other.Score);
        if (scoreComparison != 0) return scoreComparison;
        
        // Tie-breaker 1: No namespace (0) comes before Has namespace (1)
        int penaltyComparison = Penalty.CompareTo(other.Penalty);
        if (penaltyComparison != 0) return penaltyComparison;

        // Tie-breaker 2: Alphabetical
        return string.Compare(NameForSorting, other.NameForSorting, StringComparison.Ordinal);
    }
}

// Thread-safe wrapper to return both the filtered results and the new progressive pool to the Main Thread
public struct SearchTaskResult<TResult, TCandidate>
{
    public List<TResult> Results;
    public List<TCandidate> ProgressivePool;
}

// ==========================================
// OPTIMIZATION: TOP-K BOUNDED LIST
// ==========================================
public class TopKList<T>
{
    private ScoredItem<T>[] _items;
    private int _count;
    private int _k;

    public TopKList(int k)
    {
        _k = k;
        _items = new ScoredItem<T>[k];
        _count = 0;
    }

    public void Add(ScoredItem<T> item)
    {
        // Ignore immediately if array is full and the item's score is worse than the worst item
        if (_count == _k && item.CompareTo(_items[_count - 1]) >= 0) return;

        int insertIndex = _count < _k ? _count : _k - 1;
        
        while (insertIndex > 0 && item.CompareTo(_items[insertIndex - 1]) < 0)
        {
            if (insertIndex < _k) _items[insertIndex] = _items[insertIndex - 1];
            insertIndex--;
        }
        
        if (insertIndex < _k)
        {
            _items[insertIndex] = item;
            if (_count < _k) _count++;
        }
    }
    
    public List<T> GetResult()
    {
        var res = new List<T>(_count);
        for(int i = 0; i < _count; i++) res.Add(_items[i].Item);
        return res;
    }
}

// ==========================================
// 1. THE MODEL
// Holds all the data and state of the window.
// ==========================================
public enum SearchMode { Component, Layer, Tag, Name } 

public class ObjectFinderModel
{
    public string BackupSearchQuery = "";
    public bool KeepResultsOnClear = false;
    public static bool AutoFocusEnabled = false;
    public SearchMode CurrentSearchMode = SearchMode.Component;
    public string SearchQuery = "";
    
    // NEW: Progressive Filtering State
    public string PreviousCleanQuery = "";
    public List<ComponentCandidate> ProgressiveComponents = null;
    public List<StringCandidate> ProgressiveLayers = null;
    public List<StringCandidate> ProgressiveTags = null;
    public List<SceneCandidate> ProgressiveScene = null;

    // Static Cache State (Survives window close, builds on compile)
    public static bool IsComponentCacheBuilt = false;
    public static List<Type> StaticAllComponentTypes = new List<Type>();
    public static List<ComponentCandidate> StaticComponentCandidates = new List<ComponentCandidate>();

    public List<Type> AllComponentTypes => StaticAllComponentTypes;
    public List<ComponentCandidate> ComponentCandidates => StaticComponentCandidates;

    public Type SelectedType = null;
    public List<Type> FilteredTypes = new List<Type>();
    
    public static List<Type> ComponentHistory = new List<Type>();
    public bool IsShowingHistory = false;
    
    public string SelectedLayerName = null;
    public List<string> AllLayers = new List<string>();
    public List<string> FilteredLayers = new List<string>();
    public List<StringCandidate> LayerCandidates = new List<StringCandidate>();

    public string SelectedTagName = null;
    public List<string> AllTags = new List<string>();
    public List<string> FilteredTags = new List<string>();
    public List<StringCandidate> TagCandidates = new List<StringCandidate>();
    
    public List<GameObject> FoundGameObjects = new List<GameObject>();
    public Vector2 TypesScrollPos;
    public Vector2 ObjectsScrollPos;

    public bool NeedsFocus = true; 
    public int HighlightedIndex = 0; 
    public int ResultHighlightedIndex = 0; 
    public HashSet<int> SelectedResultIndices = new HashSet<int>(); 
    public int LastAnchorIndex = -1;

    public bool IsDropdownOpen = false;
    public Rect SearchBarRect; 
    public Rect DropdownRect;

    public List<SceneCandidate> SceneCandidates = new List<SceneCandidate>(); 
    public System.Threading.CancellationTokenSource SearchCTS;
    public bool IsSceneCacheDirty = true; 

    public bool HasSelection
    {
        get
        {
            if (CurrentSearchMode == SearchMode.Component) return SelectedType != null;
            if (CurrentSearchMode == SearchMode.Layer) return !string.IsNullOrEmpty(SelectedLayerName);
            if (CurrentSearchMode == SearchMode.Tag) return !string.IsNullOrEmpty(SelectedTagName);
            if (CurrentSearchMode == SearchMode.Name) return !string.IsNullOrEmpty(SearchQuery); 
            return false;
        }
    }

    public string CurrentSelectionName
    {
        get
        {
            if (CurrentSearchMode == SearchMode.Component) return SelectedType?.FullName ?? SelectedType?.Name;
            if (CurrentSearchMode == SearchMode.Layer) return SelectedLayerName;
            if (CurrentSearchMode == SearchMode.Tag) return SelectedTagName;
            return SearchQuery;
        }
    }
}

// ==========================================
// 2. THE CONTROLLER
// Handles the logic, data processing, and state updates.
// ==========================================
public class ObjectFinderController
{
    private ObjectFinderModel _model;
    private const int MAX_HISTORY = 7;
    public Action OnRepaintRequested; 

    private int _lastTagCount = -1;
    private int _lastLayerCount = -1;
    private bool _refreshPending = false;
    private double _nextRefreshTime = 0;

    public ObjectFinderController(ObjectFinderModel model)
    {
        _model = model;
        
        PrewarmComponentCache(); 
        
        CacheAllLayers();
        CacheAllTags();
    }

    [InitializeOnLoadMethod]
    public static void PrewarmComponentCache()
    {
        if (ObjectFinderModel.IsComponentCacheBuilt) return;

        List<Type> rawTypes = new List<Type>();

#if UNITY_2019_2_OR_NEWER
        var extractedTypes = TypeCache.GetTypesDerivedFrom<Component>();
        foreach (var t in extractedTypes)
        {
            if (!t.IsAbstract) rawTypes.Add(t);
        }
#else
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (Assembly assembly in assemblies)
        {
            string name = assembly.GetName().Name;
            bool isSystem = name.StartsWith("System") || name.StartsWith("mscorlib") || name.StartsWith("Mono.") || name.StartsWith("Newtonsoft") || name.StartsWith("nunit") || name.StartsWith("ExCSS") || name.StartsWith("Microsoft") || name.StartsWith("Bee.") || name.StartsWith("PlayerBuildProgramLibrary") || name.StartsWith("ScriptCompilationBuildProgram");
            if (isSystem) continue;

            Type[] types = null;
            try { types = assembly.GetTypes(); } catch (ReflectionTypeLoadException e) { types = e.Types; } catch { continue; }
            if (types == null) continue;

            foreach (Type type in types)
            {
                if (type != null && type.IsSubclassOf(typeof(Component)) && !type.IsAbstract) rawTypes.Add(type);
            }
        }
#endif

        var candidates = new List<ComponentCandidate>(rawTypes.Count);
        foreach (var t in rawTypes)
        {
            candidates.Add(new ComponentCandidate {
                Type = t,
                Data = new SearchData(t.Name, t.FullName)
            });
        }

        ObjectFinderModel.StaticAllComponentTypes = rawTypes.OrderBy(t => t.Name).ToList();
        ObjectFinderModel.StaticComponentCandidates = candidates.OrderBy(c => c.Type.Name).ToList();
        ObjectFinderModel.IsComponentCacheBuilt = true;
    }

    private string[] _cachedLayerNames = new string[32];
    private int _lastSelectionComponentCount = -1;
private GameObject _lastSelectedGameObject = null;


    public void RequestDeferredRefresh()
    {
        if (!_model.HasSelection) return;
        _refreshPending = true;
        _nextRefreshTime = EditorApplication.timeSinceStartup + 0.25f; // 250ms delay to batch rapid changes
    }

    public void OnEditorUpdate()
    {
        // 1. Instantly prune destroyed "zombie" objects from the list so it reorders automatically
        if (_model.HasSelection && _model.FoundGameObjects.Count > 0)
        {
            bool removedAny = false;
            var survivingObjects = new List<GameObject>(_model.FoundGameObjects.Count);
            var newSelectedIndices = new HashSet<int>();

            for (int i = 0; i < _model.FoundGameObjects.Count; i++)
            {
                GameObject go = _model.FoundGameObjects[i];
                if (go == null)
                {
                    removedAny = true;
                }
                else
                {
                    if (_model.SelectedResultIndices.Contains(i))
                    {
                        newSelectedIndices.Add(survivingObjects.Count);
                    }
                    survivingObjects.Add(go);
                }
            }

            if (removedAny)
            {
                _model.FoundGameObjects = survivingObjects;
                _model.SelectedResultIndices = newSelectedIndices;

                if (_model.ResultHighlightedIndex >= _model.FoundGameObjects.Count)
                {
                    _model.ResultHighlightedIndex = Mathf.Max(0, _model.FoundGameObjects.Count - 1);
                }

                if (!_model.SelectedResultIndices.Contains(_model.LastAnchorIndex))
                {
                    _model.LastAnchorIndex = -1;
                }

                OnRepaintRequested?.Invoke(); 
            }
        }

        GameObject currentSelection = UnityEditor.Selection.activeGameObject;
        if (currentSelection != _lastSelectedGameObject)
        {
            _lastSelectedGameObject = currentSelection;
            _lastSelectionComponentCount = currentSelection != null ? currentSelection.GetComponents<Component>().Length : -1;
        }
        else if (currentSelection != null)
        {
            int currentComponentCount = currentSelection.GetComponents<Component>().Length;
            if (currentComponentCount != _lastSelectionComponentCount)
            {
                _lastSelectionComponentCount = currentComponentCount;

                // If the component count changed and we are in Component search mode, trigger a refresh!
                if (_model.CurrentSearchMode == SearchMode.Component)
                {
                    RequestDeferredRefresh();
                }
            }
        }

        // 3. Process any pending deferred refreshes (from name changes, layer changes, etc.)
        if (_refreshPending && EditorApplication.timeSinceStartup >= _nextRefreshTime)
        {
            _refreshPending = false;
            FindGameObjects();
            OnRepaintRequested?.Invoke();
        }
    }
    

    public UnityEditor.UndoPropertyModification[] OnPostprocessModifications(UnityEditor.UndoPropertyModification[] modifications)
    {
        if (_model.HasSelection)
        {
            for (int i = 0; i < modifications.Length; i++)
            {
                var path = modifications[i].currentValue?.propertyPath;
                // Trigger a refresh if Layer, Tag, Name, or active state is tweaked in the inspector
                if (path == "m_Layer" || path == "m_TagString" || path == "m_Name" || path == "m_IsActive")
                {
                    RequestDeferredRefresh();
                    break;
                }
            }
        }
        return modifications;
    }


    public void CheckTagsAndLayers()
    {
        // 1. Check for Layer Name Changes
        bool layerChanged = false;
        for (int i = 0; i <= 31; i++)
        {
            string currentName = LayerMask.LayerToName(i);
            if (currentName != _cachedLayerNames[i])
            {
                if (_model.CurrentSearchMode == SearchMode.Layer && _model.SelectedLayerName == _cachedLayerNames[i])
                {
                    _model.SelectedLayerName = currentName;
                    _model.SearchQuery = currentName;
                }
                layerChanged = true;
            }
        }

        // 2. Check for Tag Changes (More robust than just counting)
        var tags = UnityEditorInternal.InternalEditorUtility.tags;
        bool tagsChanged = _lastTagCount != tags.Length;

        // If the count is the same, check if any strings actually changed (e.g., renamed)
        if (!tagsChanged && _model.AllTags.Count == tags.Length)
        {
            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] != _model.AllTags[i])
                {
                    tagsChanged = true;
                    break;
                }
            }
        }

        if (layerChanged || tagsChanged)
        {
            if (layerChanged) CacheAllLayers();
            if (tagsChanged)
            {
                CacheAllTags();

                // SECURITY CHECK: Did they delete the tag we are currently searching for?
                if (_model.CurrentSearchMode == SearchMode.Tag && !string.IsNullOrEmpty(_model.SelectedTagName))
                {
                    if (!_model.AllTags.Contains(_model.SelectedTagName))
                    {
                        // Tag is gone! Clear the search so Unity doesn't throw a "Tag not defined" exception.
                        ClearSelection();
                    }
                }
            }

            // Refresh the results list so the labels update to the new names
            if (_model.HasSelection)
            {
                FindGameObjects();
            }

            OnRepaintRequested?.Invoke();
        }
    }



    public void MarkSceneCacheDirty() { _model.IsSceneCacheDirty = true; }

    public void RefreshSceneCacheIfNeeded()
    {
        if (_model.CurrentSearchMode != SearchMode.Name) return; 
        if (!_model.IsSceneCacheDirty) return; 

        var allGos = Object.FindObjectsOfType<GameObject>(true);
        
        if (_model.SceneCandidates.Capacity < allGos.Length)
            _model.SceneCandidates.Capacity = allGos.Length;
            
        _model.SceneCandidates.Clear();
        _model.ProgressiveScene = null;
        _model.PreviousCleanQuery = "";
        
        for (int i = 0; i < allGos.Length; i++)
        {
            _model.SceneCandidates.Add(new SceneCandidate { 
                GameObject = allGos[i], 
                Data = new SearchData(allGos[i].name, allGos[i].name) 
            });
        }
        
        _model.IsSceneCacheDirty = false;
    }

    private void CacheAllLayers()
    {
        _model.AllLayers.Clear();
        _model.LayerCandidates.Clear();

        for (int i = 0; i <= 31; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            _cachedLayerNames[i] = layerName; // Store name by index

            if (!string.IsNullOrEmpty(layerName))
            {
                _model.AllLayers.Add(layerName);
                _model.LayerCandidates.Add(new StringCandidate { Value = layerName, Data = new SearchData(layerName, layerName) });
            }
        }
        _lastLayerCount = _model.AllLayers.Count;
    }


    private void CacheAllTags()
    {
        _model.AllTags.Clear();
        _model.TagCandidates.Clear();
        
        var tags = UnityEditorInternal.InternalEditorUtility.tags;
        _lastTagCount = tags.Length;

        foreach (string tag in tags)
        {
            _model.AllTags.Add(tag);
            _model.TagCandidates.Add(new StringCandidate { Value = tag, Data = new SearchData(tag, tag) });
        }
    }

    public void ChangeSearchMode(SearchMode newMode)
    {
        if (_model.CurrentSearchMode != newMode)
        {
            _model.CurrentSearchMode = newMode;
            ClearSelection();

            if (newMode == SearchMode.Component) ShowComponentHistory();
            if (newMode == SearchMode.Layer) ShowAllLayers();
            if (newMode == SearchMode.Tag) ShowAllTags();
            if (newMode == SearchMode.Name) RefreshSceneCacheIfNeeded();
        }
    }
    
    public void RemoveFromHistory(Type type)
    {
        if (ObjectFinderModel.ComponentHistory.Contains(type))
        {
            ObjectFinderModel.ComponentHistory.Remove(type);
            ShowComponentHistory(); 
        }
    }

    public void ShowAllLayers()
    {
        _model.FilteredLayers = new List<string>(_model.AllLayers);
        _model.IsDropdownOpen = true;
        _model.HighlightedIndex = 0;
    }

    public void ShowAllTags()
    {
        _model.FilteredTags = new List<string>(_model.AllTags);
        _model.IsDropdownOpen = true;
        _model.HighlightedIndex = 0;
    }

    public void ShowComponentHistory()
    {
        if (ObjectFinderModel.ComponentHistory.Count > 0)
        {
            _model.FilteredTypes = new List<Type>(ObjectFinderModel.ComponentHistory);
            _model.FilteredTypes.Reverse(); 
            _model.IsDropdownOpen = true;
            _model.IsShowingHistory = true;
            _model.HighlightedIndex = 0;
        }
        else
        {
            _model.IsDropdownOpen = false;
        }
    }

    public void CancelSearch()
    {
        if (!string.IsNullOrEmpty(_model.BackupSearchQuery))
        {
            string restoredQuery = _model.BackupSearchQuery;
            _model.BackupSearchQuery = "";
            _model.SearchQuery = restoredQuery;

            if (_model.CurrentSearchMode == SearchMode.Name)
            {
                // Re-run the name search silently to restore the original object list
                UpdateSearchQuery(restoredQuery);
            }
            else
            {
                // In other modes, the list isn't tied to the text, so just close the dropdown
                _model.IsDropdownOpen = false;
            }
        }
        else
        {
            _model.IsDropdownOpen = false;
            if (_model.HasSelection)
                _model.SearchQuery = _model.CurrentSelectionName;
        }
    }

    public async void UpdateSearchQuery(string newQuery)
    {
        if (_model.SearchQuery == newQuery) return;

        _model.SearchQuery = newQuery;
        _model.HighlightedIndex = 0;
        _model.IsShowingHistory = false;

        _model.SearchCTS?.Cancel();
        _model.SearchCTS = new System.Threading.CancellationTokenSource();
        var token = _model.SearchCTS.Token;

        if (string.IsNullOrWhiteSpace(newQuery))
        {
            _model.FilteredTypes.Clear();
            _model.FilteredLayers.Clear();
            _model.FilteredTags.Clear();

            _model.PreviousCleanQuery = "";
            _model.ProgressiveComponents = null;
            _model.ProgressiveLayers = null;
            _model.ProgressiveTags = null;
            _model.ProgressiveScene = null;

            if (_model.CurrentSearchMode == SearchMode.Component) ShowComponentHistory();
            else if (_model.CurrentSearchMode == SearchMode.Layer) ShowAllLayers();
            else if (_model.CurrentSearchMode == SearchMode.Tag) ShowAllTags();
            else
            {
                _model.IsDropdownOpen = false;

                // CHANGED: Only wipe the FoundGameObjects if we aren't explicitly retaining them
                if (!_model.KeepResultsOnClear)
                {
                    _model.FoundGameObjects.Clear();
                }
            }
            return;
        }

        try
        {
            await System.Threading.Tasks.Task.Delay(15, token);

            if (token.IsCancellationRequested) return;

            if (_model.CurrentSearchMode == SearchMode.Name)
            {
                _model.IsDropdownOpen = false;
                RefreshSceneCacheIfNeeded();
                await FindGameObjectsByNameAsync(token);
                OnRepaintRequested?.Invoke();
                return;
            }

            _model.IsDropdownOpen = true;

            string cleanQuery = newQuery.Replace(" ", "").ToLower();
            string[] searchTerms = newQuery.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            bool useProgressive = cleanQuery.StartsWith(_model.PreviousCleanQuery) && _model.PreviousCleanQuery.Length > 0;

            if (_model.CurrentSearchMode == SearchMode.Component)
            {
                var pool = (useProgressive && _model.ProgressiveComponents != null) ? _model.ProgressiveComponents : _model.ComponentCandidates;

                var taskResult = await System.Threading.Tasks.Task.Run(() =>
                {
                    var res = new SearchTaskResult<Type, ComponentCandidate>();
                    var nextProg = new List<ComponentCandidate>(pool.Count);
                    var topK = new TopKList<Type>(50);

                    for (int i = 0; i < pool.Count; i++)
                    {
                        if (token.IsCancellationRequested) return res;
                        int score = GetMatchScore(pool[i].Data, cleanQuery, searchTerms);
                        if (score < int.MaxValue)
                        {
                            nextProg.Add(pool[i]);
                            topK.Add(new ScoredItem<Type> { 
                                Item = pool[i].Type, 
                                Score = score, 
                                Penalty = string.IsNullOrEmpty(pool[i].Type.Namespace) ? 0 : 1, // Deprioritize namespaces
                                NameForSorting = pool[i].Data.OriginalName 
                            });
                        }
                    }
                    res.Results = topK.GetResult();
                    res.ProgressivePool = nextProg;
                    return res;
                }, token);

                if (token.IsCancellationRequested || taskResult.Results == null) return;

                _model.FilteredTypes = taskResult.Results;
                _model.ProgressiveComponents = taskResult.ProgressivePool;
            }
            else if (_model.CurrentSearchMode == SearchMode.Layer)
            {
                var pool = (useProgressive && _model.ProgressiveLayers != null) ? _model.ProgressiveLayers : _model.LayerCandidates;

                var taskResult = await System.Threading.Tasks.Task.Run(() =>
                {
                    var res = new SearchTaskResult<string, StringCandidate>();
                    var nextProg = new List<StringCandidate>(pool.Count);
                    var topK = new TopKList<string>(50);

                    for (int i = 0; i < pool.Count; i++)
                    {
                        if (token.IsCancellationRequested) return res;
                        int score = GetMatchScore(pool[i].Data, cleanQuery, searchTerms);
                        if (score < int.MaxValue)
                        {
                            nextProg.Add(pool[i]);
                            topK.Add(new ScoredItem<string> { Item = pool[i].Value, Score = score, NameForSorting = pool[i].Data.OriginalName });
                        }
                    }
                    res.Results = topK.GetResult();
                    res.ProgressivePool = nextProg;
                    return res;
                }, token);

                if (token.IsCancellationRequested || taskResult.Results == null) return;

                _model.FilteredLayers = taskResult.Results;
                _model.ProgressiveLayers = taskResult.ProgressivePool;
            }
            else if (_model.CurrentSearchMode == SearchMode.Tag)
            {
                var pool = (useProgressive && _model.ProgressiveTags != null) ? _model.ProgressiveTags : _model.TagCandidates;

                var taskResult = await System.Threading.Tasks.Task.Run(() =>
                {
                    var res = new SearchTaskResult<string, StringCandidate>();
                    var nextProg = new List<StringCandidate>(pool.Count);
                    var topK = new TopKList<string>(50);

                    for (int i = 0; i < pool.Count; i++)
                    {
                        if (token.IsCancellationRequested) return res;
                        int score = GetMatchScore(pool[i].Data, cleanQuery, searchTerms);
                        if (score < int.MaxValue)
                        {
                            nextProg.Add(pool[i]);
                            topK.Add(new ScoredItem<string> { Item = pool[i].Value, Score = score, NameForSorting = pool[i].Data.OriginalName });
                        }
                    }
                    res.Results = topK.GetResult();
                    res.ProgressivePool = nextProg;
                    return res;
                }, token);

                if (token.IsCancellationRequested || taskResult.Results == null) return;

                _model.FilteredTags = taskResult.Results;
                _model.ProgressiveTags = taskResult.ProgressivePool;
            }

            _model.PreviousCleanQuery = cleanQuery;
            OnRepaintRequested?.Invoke();
        }
        catch (System.OperationCanceledException) { }
    }


    private async System.Threading.Tasks.Task FindGameObjectsByNameAsync(System.Threading.CancellationToken token)
    {
        string cleanQuery = _model.SearchQuery.Replace(" ", "").ToLower();
        string[] searchTerms = _model.SearchQuery.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        bool useProgressive = cleanQuery.StartsWith(_model.PreviousCleanQuery) && _model.PreviousCleanQuery.Length > 0;
        var pool = (useProgressive && _model.ProgressiveScene != null) ? _model.ProgressiveScene : _model.SceneCandidates;

        var taskResult = await System.Threading.Tasks.Task.Run(() =>
        {
            var res = new SearchTaskResult<GameObject, SceneCandidate>();
            var nextProg = new List<SceneCandidate>(pool.Count);
            var results = new List<ScoredItem<GameObject>>(pool.Count); // Full array sort so we don't break "Select All"

            for (int i = 0; i < pool.Count; i++)
            {
                if (token.IsCancellationRequested) return res;

                int score = GetMatchScore(pool[i].Data, cleanQuery, searchTerms);
                if (score < int.MaxValue && pool[i].GameObject != null)
                {
                    nextProg.Add(pool[i]);
                    results.Add(new ScoredItem<GameObject>
                    {
                        Item = pool[i].GameObject,
                        Score = score,
                        NameForSorting = pool[i].Data.OriginalName
                    });
                }
            }
            results.Sort();

            res.ProgressivePool = nextProg;
            res.Results = new List<GameObject>(results.Count);
            for (int i = 0; i < results.Count; i++) res.Results.Add(results[i].Item);

            return res;
        }, token);

        if (token.IsCancellationRequested || taskResult.Results == null) return;

        _model.FoundGameObjects = taskResult.Results;
        _model.ProgressiveScene = taskResult.ProgressivePool;
        _model.PreviousCleanQuery = cleanQuery;

        _model.ResultHighlightedIndex = 0;
        _model.SelectedResultIndices.Clear();
        _model.LastAnchorIndex = -1;
    }
    
    private int GetMatchScore(SearchData data, string cleanQuery, string[] searchTerms)
    {
        if (data.LowerName.StartsWith(cleanQuery)) return 1;

        bool standardMatch = true;
        for (int i = 0; i < searchTerms.Length; i++)
        {
            if (data.LowerFullName.IndexOf(searchTerms[i], StringComparison.OrdinalIgnoreCase) < 0)
            {
                standardMatch = false;
                break;
            }
        }
        if (standardMatch) return 2;

        if (data.LowerName.Contains(cleanQuery)) return 3;

        if (cleanQuery.Length >= 2 && cleanQuery.Length <= 5)
        {
            if (data.Acronym.StartsWith(cleanQuery)) return 4;
        }

        if (cleanQuery.Length > 1)
        {
            int qIdx = 0;
            for (int tIdx = 0; tIdx < data.LowerName.Length && qIdx < cleanQuery.Length; tIdx++)
            {
                if (cleanQuery[qIdx] == data.LowerName[tIdx]) qIdx++;
            }
            if (qIdx == cleanQuery.Length) return 5;
        }

        if (cleanQuery.Length > 3 && cleanQuery.Length <= data.OriginalName.Length + 1)
        {
            bool mashupMatch = true;
            for (int i = 0; i < data.ClassWordsLower.Count; i++)
            {
                if (cleanQuery.IndexOf(data.ClassWordsLower[i]) < 0)
                {
                    mashupMatch = false;
                    break;
                }
            }
            if (mashupMatch) return 6;
        }

        if (cleanQuery.Length > 3)
        {
            string tempQuery = cleanQuery;
            for (int i = 0; i < data.ClassWordsLower.Count; i++)
            {
                string word = data.ClassWordsLower[i];
                if (tempQuery.Contains(word)) tempQuery = tempQuery.Replace(word, "");
            }
            if (tempQuery.Length == 0) return 7;
        }

        if (cleanQuery.Length > 5)
        {
            int matchedLength = 0;
            for (int i = 0; i < data.ClassWordsLower.Count; i++)
            {
                string word = data.ClassWordsLower[i];
                int wordErrorsAllowed = word.Length >= 4 ? 1 : 0;
                if (FuzzyContainsSubString(cleanQuery, word, wordErrorsAllowed))
                {
                    matchedLength += word.Length;
                }
            }
            int allowedOverallErrors = cleanQuery.Length >= 10 ? 2 : 1;
            if (matchedLength > 0 && Math.Abs(cleanQuery.Length - matchedLength) <= allowedOverallErrors) return 8;
        }

        if (cleanQuery.Length >= 4)
        {
            int allowedErrors = cleanQuery.Length >= 10 ? 2 : 1;
            if (FuzzyContainsSubString(data.LowerName, cleanQuery, allowedErrors)) return 9;
        }

        return int.MaxValue; 
    }

    private bool FuzzyContainsSubString(string text, string pattern, int maxErrors)
    {
        if (pattern.Length > text.Length + maxErrors) return false;

        Span<int> dp = stackalloc int[pattern.Length + 1];
        
        for (int i = 0; i <= pattern.Length; i++) dp[i] = i;

        for (int i = 1; i <= text.Length; i++)
        {
            int prev = dp[0];
            dp[0] = 0; 
            for (int j = 1; j <= pattern.Length; j++)
            {
                int temp = dp[j];
                if (text[i - 1] == pattern[j - 1]) dp[j] = prev;
                else dp[j] = 1 + Math.Min(prev, Math.Min(dp[j], dp[j - 1])); 
                prev = temp;
            }
            if (dp[pattern.Length] <= maxErrors) return true;
        }
        return false;
    }

    private void AddToHistory(Type type)
    {
        if (ObjectFinderModel.ComponentHistory.Contains(type)) ObjectFinderModel.ComponentHistory.Remove(type);
        ObjectFinderModel.ComponentHistory.Add(type);
        if (ObjectFinderModel.ComponentHistory.Count > MAX_HISTORY) ObjectFinderModel.ComponentHistory.RemoveAt(0);
    }

    public void SelectComponentType(Type type)
    {
        _model.SearchCTS?.Cancel(); 
        _model.SelectedType = type;
        _model.SearchQuery = type.FullName ?? type.Name; 
        _model.BackupSearchQuery = ""; // Clear backup on successful selection
        AddToHistory(type); 
        _model.FilteredTypes.Clear();
        _model.IsDropdownOpen = false; 
        _model.IsShowingHistory = false;
        FindGameObjects();
    }

    public void SelectLayer(string layerName)
    {
        _model.SearchCTS?.Cancel(); 
        _model.SelectedLayerName = layerName;
        _model.SearchQuery = layerName;
        _model.BackupSearchQuery = ""; // Clear backup on successful selection
        _model.FilteredLayers.Clear();
        _model.IsDropdownOpen = false;
        FindGameObjects();
    }

    public void SelectTag(string tagName)
    {
        _model.SearchCTS?.Cancel(); 
        _model.SelectedTagName = tagName;
        _model.SearchQuery = tagName;
        _model.BackupSearchQuery = ""; // Clear backup on successful selection
        _model.FilteredTags.Clear();
        _model.IsDropdownOpen = false;
        FindGameObjects();
    }

    public void ClearSelection()
    {
        _model.SelectedType = null;
        _model.SelectedLayerName = null;
        _model.SelectedTagName = null;
        _model.SearchQuery = "";

        _model.BackupSearchQuery = "";
        _model.KeepResultsOnClear = false;

        _model.PreviousCleanQuery = "";
        _model.ProgressiveComponents = null;
        _model.ProgressiveLayers = null;
        _model.ProgressiveTags = null;
        _model.ProgressiveScene = null;

        _model.FilteredTypes.Clear();
        _model.FilteredLayers.Clear();
        _model.FilteredTags.Clear();
        _model.FoundGameObjects.Clear();
        _model.NeedsFocus = true;
        _model.ResultHighlightedIndex = 0;
        _model.SelectedResultIndices.Clear();
        _model.LastAnchorIndex = -1;
        _model.IsDropdownOpen = false;
        _model.IsShowingHistory = false;
    }


    public void FindGameObjects()
    {
        // 1. Remember currently selected GameObjects to restore them after the refresh
        var previouslySelected = new HashSet<GameObject>();
        foreach (var index in _model.SelectedResultIndices)
        {
            if (index >= 0 && index < _model.FoundGameObjects.Count && _model.FoundGameObjects[index] != null)
                previouslySelected.Add(_model.FoundGameObjects[index]);
        }

        _model.FoundGameObjects.Clear();
        _model.ResultHighlightedIndex = 0;
        _model.SelectedResultIndices.Clear();
        _model.LastAnchorIndex = -1;

        if (_model.CurrentSearchMode == SearchMode.Component)
        {
            if (_model.SelectedType == null) return;
            Object[] foundComponents = Object.FindObjectsOfType(_model.SelectedType, true);
            foreach (Object comp in foundComponents)
            {
                // ADDED: "comp != null" to prevent adding destroyed components
                if (comp != null && comp is Component c && c.gameObject != null)
                    _model.FoundGameObjects.Add(c.gameObject);
            }
        }
        else if (_model.CurrentSearchMode == SearchMode.Layer)
        {
            if (string.IsNullOrEmpty(_model.SelectedLayerName)) return;
            int targetLayer = LayerMask.NameToLayer(_model.SelectedLayerName);
            GameObject[] allGameObjects = Object.FindObjectsOfType<GameObject>(true);
            foreach (GameObject go in allGameObjects)
            {
                if (go != null && go.layer == targetLayer)
                    _model.FoundGameObjects.Add(go);
            }
        }
        else if (_model.CurrentSearchMode == SearchMode.Tag)
        {
            if (string.IsNullOrEmpty(_model.SelectedTagName)) return;
            GameObject[] allGameObjects = Object.FindObjectsOfType<GameObject>(true);
            foreach (GameObject go in allGameObjects)
            {
                if (go != null && go.CompareTag(_model.SelectedTagName))
                {
                    _model.FoundGameObjects.Add(go);
                }
            }
        }
        else if (_model.CurrentSearchMode == SearchMode.Name)
        {
            if (!string.IsNullOrEmpty(_model.SearchQuery))
            {
                UpdateSearchQuery(_model.SearchQuery);
            }
        }

        if (_model.CurrentSearchMode != SearchMode.Name && _model.FoundGameObjects.Count > 1)
        {
            // Using CompareOrdinal is the absolute fastest, zero-allocation way to sort strings in C#
            _model.FoundGameObjects.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        // 2. Restore previous selection
        for (int i = 0; i < _model.FoundGameObjects.Count; i++)
        {
            if (previouslySelected.Contains(_model.FoundGameObjects[i]))
            {
                _model.SelectedResultIndices.Add(i);
            }
        }
    }
    

    public void MoveHighlightUp() { if (_model.HighlightedIndex > 0) _model.HighlightedIndex--; }

    public void MoveHighlightDown()
    {
        int maxItems = 0;
        if (_model.CurrentSearchMode == SearchMode.Component) maxItems = _model.FilteredTypes.Count;
        else if (_model.CurrentSearchMode == SearchMode.Layer) maxItems = _model.FilteredLayers.Count;
        else if (_model.CurrentSearchMode == SearchMode.Tag) maxItems = _model.FilteredTags.Count;
        int maxIndex = Mathf.Max(0, Mathf.Min(maxItems, 50) - 1);
        if (_model.HighlightedIndex < maxIndex) _model.HighlightedIndex++;
    }

    public void SelectHighlightedSuggestion()
    {
        if (_model.CurrentSearchMode == SearchMode.Component)
        {
            if (_model.FilteredTypes.Count > 0 && _model.HighlightedIndex < _model.FilteredTypes.Count) SelectComponentType(_model.FilteredTypes[_model.HighlightedIndex]);
        }
        else if (_model.CurrentSearchMode == SearchMode.Layer)
        {
            if (_model.FilteredLayers.Count > 0 && _model.HighlightedIndex < _model.FilteredLayers.Count) SelectLayer(_model.FilteredLayers[_model.HighlightedIndex]);
        }
        else if (_model.CurrentSearchMode == SearchMode.Tag)
        {
            if (_model.FilteredTags.Count > 0 && _model.HighlightedIndex < _model.FilteredTags.Count) SelectTag(_model.FilteredTags[_model.HighlightedIndex]);
        }
    }

    public void MoveResultHighlightUp() { if (_model.ResultHighlightedIndex > 0) _model.ResultHighlightedIndex--; }

    public void MoveResultHighlightDown() { if (_model.ResultHighlightedIndex < _model.FoundGameObjects.Count - 1) _model.ResultHighlightedIndex++; }

    public void SelectResult(int index, bool multiSelect, bool shiftSelect)
    {
        if (index < 0 || index >= _model.FoundGameObjects.Count) return;
        _model.ResultHighlightedIndex = index; 

        if (shiftSelect && _model.LastAnchorIndex != -1 && _model.LastAnchorIndex < _model.FoundGameObjects.Count)
        {
            if (!multiSelect) _model.SelectedResultIndices.Clear();
            int startIndex = Mathf.Min(_model.LastAnchorIndex, index);
            int endIndex = Mathf.Max(_model.LastAnchorIndex, index);
            for (int i = startIndex; i <= endIndex; i++) _model.SelectedResultIndices.Add(i);
        }
        else if (multiSelect)
        {
            if (_model.SelectedResultIndices.Contains(index)) _model.SelectedResultIndices.Remove(index);
            else _model.SelectedResultIndices.Add(index);
            _model.LastAnchorIndex = index; 
        }
        else
        {
            _model.SelectedResultIndices.Clear();
            _model.SelectedResultIndices.Add(index);
            _model.LastAnchorIndex = index; 
        }
        SyncUnitySelection();
    }

    public void SelectAllResults()
    {
        _model.SelectedResultIndices.Clear();
        for (int i = 0; i < _model.FoundGameObjects.Count; i++) _model.SelectedResultIndices.Add(i);
        SyncUnitySelection();
    }

    // ==========================================
    // INSPECTOR SCROLLING STATE & CACHE
    // ==========================================
    private EditorWindow _cachedInspectorWindow;
    private Type _inspectorWindowType;
    private int _scrollWaitFrames = 0;
    private Component _targetScrollComponent = null;


    private EditorWindow GetInspectorWindow()
    {
        if (_cachedInspectorWindow != null) return _cachedInspectorWindow;

        if (_inspectorWindowType == null)
            _inspectorWindowType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");

        if (_inspectorWindowType == null) return null;

        var windows = Resources.FindObjectsOfTypeAll(_inspectorWindowType);
        if (windows.Length > 0)
        {
            _cachedInspectorWindow = (EditorWindow)windows[0];
            return _cachedInspectorWindow;
        }
        return null;
    }

    private void SyncUnitySelection()
    {
        List<Object> objectsToSelect = new List<Object>();
        foreach (int index in _model.SelectedResultIndices)
        {
            if (index < _model.FoundGameObjects.Count && _model.FoundGameObjects[index] != null)
                objectsToSelect.Add(_model.FoundGameObjects[index]);
        }
        Selection.objects = objectsToSelect.ToArray();

        if (_model.CurrentSearchMode == SearchMode.Component && 
            _model.SelectedType != null && 
            _model.SelectedResultIndices.Count == 1)
        {
            int selectedIndex = _model.SelectedResultIndices.First();
            GameObject selectedGo = _model.FoundGameObjects[selectedIndex];
            
            if (selectedGo != null)
            {
                _targetScrollComponent = selectedGo.GetComponent(_model.SelectedType);
                if (_targetScrollComponent != null)
                {
                    _scrollWaitFrames = 15;
                    EditorApplication.update -= AttemptScroll;
                    EditorApplication.update += AttemptScroll;
                }
            }
        }
    }

    private void AttemptScroll()
    {
        _scrollWaitFrames--;
        
        if (_scrollWaitFrames <= 0 || _targetScrollComponent == null)
        {
            EditorApplication.update -= AttemptScroll;
            return;
        }

        bool success = TryScrollInspectorToComponent(_targetScrollComponent);
        if (success)
        {
            EditorApplication.update -= AttemptScroll;
        }
    }

    private bool TryScrollInspectorToComponent(Component targetComponent)
    {
        EditorWindow inspector = GetInspectorWindow();
        if (inspector == null) return false;

        var tracker = ActiveEditorTracker.sharedTracker;
        var editors = tracker.activeEditors;
        int editorIndex = -1;

        for (int i = 0; i < editors.Length; i++)
        {
            if (editors[i].target == targetComponent)
            {
                editorIndex = i;
                break;
            }
        }

        if (editorIndex == -1) return false;

        if (tracker.GetVisible(editorIndex) == 0)
        {
            tracker.SetVisible(editorIndex, 1);
            inspector.Repaint();
            return false;
        }
        
        var root = inspector.rootVisualElement;
        if (root != null)
        {
            var scrollView = root.Q<UnityEngine.UIElements.ScrollView>();
            if (scrollView != null)
            {
                var editorElements = scrollView.Query<UnityEngine.UIElements.VisualElement>(className: "unity-inspector-element").ToList();
                UnityEngine.UIElements.VisualElement targetElement = null;

                if (editorIndex < editorElements.Count)
                {
                    targetElement = editorElements[editorIndex];
                }
                else if (editorIndex < scrollView.contentContainer.childCount)
                {
                    targetElement = scrollView.contentContainer[editorIndex];
                }

                if (targetElement != null)
                {
                    if (float.IsNaN(targetElement.layout.y) || targetElement.layout.width == 0)
                        return false;

                    float targetY = targetElement.worldBound.y - scrollView.contentContainer.worldBound.y;
       
                    targetY -= 26f;
                    targetY -= 50f;

                    targetY = Mathf.Max(0, targetY);
                    scrollView.scrollOffset = new Vector2(scrollView.scrollOffset.x, targetY);

                    inspector.Repaint();
                    return true; 
                }
            }
        }

        FieldInfo scrollField = _inspectorWindowType.GetField("m_ScrollPosition", BindingFlags.Instance | BindingFlags.NonPublic);
        if (scrollField != null)
        {
            float estimatedY = 45f + (editorIndex * 100f);
            scrollField.SetValue(inspector, new Vector2(0, estimatedY));
            inspector.Repaint();
            return true; 
        }

        return false; 
    }
}

// ==========================================
// 3. THE VIEW
// Handles drawing the Unity Editor GUI.
// ==========================================
public class ObjectFinderView
{
    private ObjectFinderModel _model;
    private ObjectFinderController _controller;
    private EditorWindow _window; 

    public ObjectFinderView(ObjectFinderModel model, ObjectFinderController controller, EditorWindow window)
    {
        _model = model;
        _controller = controller;
        _window = window;
    }

    public void Draw()
    {
        HandleKeyboardInput(); 
        HandleMouseClicksOutsideDropdown();

        GUILayout.Space(10);
        DrawModeToggle();
        DrawSearchBar();
        GUILayout.Space(5);

        EventType originalEventType = Event.current.type;
        bool isMouseOverDropdown = _model.IsDropdownOpen && _model.DropdownRect.Contains(Event.current.mousePosition);

        if (isMouseOverDropdown)
        {
            if (originalEventType == EventType.MouseDown || 
                originalEventType == EventType.MouseUp || 
                originalEventType == EventType.ScrollWheel || 
                originalEventType == EventType.MouseDrag)
            {
                Event.current.type = EventType.Ignore;
            }
        }

        if (_model.HasSelection)
        {
            DrawResults();
        }
        else if (!_model.IsDropdownOpen)
        {
            EditorGUILayout.HelpBox($"Search for and select a {_model.CurrentSearchMode.ToString().ToLower()}...", MessageType.Info);
        }

        if (isMouseOverDropdown)
        {
            Event.current.type = originalEventType;
        }

        if (_model.IsDropdownOpen)
        {
            DrawTypeSuggestionsOverlay();
        }
    }

    private void DrawModeToggle()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Search By:", GUILayout.Width(70));
        
        EditorGUI.BeginChangeCheck();
        SearchMode newMode = (SearchMode)EditorGUILayout.EnumPopup(_model.CurrentSearchMode);
        if (EditorGUI.EndChangeCheck())
        {
            _controller.ChangeSearchMode(newMode);
            _model.NeedsFocus = true; 
            GUI.FocusControl(null);
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
    }

    private void HandleMouseClicksOutsideDropdown()
    {
        if (Event.current.type == EventType.MouseDown && _model.IsDropdownOpen)
        {
            if (!_model.SearchBarRect.Contains(Event.current.mousePosition) &&
                !_model.DropdownRect.Contains(Event.current.mousePosition))
            {
                _controller.CancelSearch(); // Use the new abort method
                GUI.FocusControl(null);
                _window.Repaint();
            }
        }
    }


    private void HandleKeyboardInput()
    {
        Event e = Event.current;

        if (e.type != EventType.KeyDown) return;

        bool isCtrlHeld = e.control || e.command;
        bool isShiftHeld = e.shift;

        if (e.keyCode == KeyCode.Escape && _model.IsDropdownOpen)
        {
            _controller.CancelSearch(); // Use the new abort method
            GUI.FocusControl(null);
            e.Use();
            return;
        }

        int currentSuggestionCount = 0;
        if (_model.CurrentSearchMode == SearchMode.Component) currentSuggestionCount = _model.FilteredTypes.Count;
        else if (_model.CurrentSearchMode == SearchMode.Layer) currentSuggestionCount = _model.FilteredLayers.Count;
        else if (_model.CurrentSearchMode == SearchMode.Tag) currentSuggestionCount = _model.FilteredTags.Count;

        if (_model.IsDropdownOpen && currentSuggestionCount > 0)
        {
            if (e.keyCode == KeyCode.DownArrow)
            {
                _controller.MoveHighlightDown();
                e.Use();
            }
            else if (e.keyCode == KeyCode.UpArrow)
            {
                _controller.MoveHighlightUp();
                e.Use();
            }
            else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                GUI.FocusControl(null);
                _controller.SelectHighlightedSuggestion();
                e.Use();
            }
        }
        else if (!_model.IsDropdownOpen && _model.HasSelection && _model.FoundGameObjects.Count > 0)
        {
            if (e.keyCode == KeyCode.DownArrow)
            {
                GUI.FocusControl(null); 
                _controller.MoveResultHighlightDown();
                e.Use();
            }
            else if (e.keyCode == KeyCode.UpArrow)
            {
                GUI.FocusControl(null); 
                _controller.MoveResultHighlightUp();
                e.Use();
            }
            else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                if (GUI.GetNameOfFocusedControl() == "ComponentSearchBox")
                {
                    _model.BackupSearchQuery = ""; // Clear backup on manual confirm
                    GUI.FocusControl(null);
                    e.Use();
                }
                else
                {
                    GUI.FocusControl(null);
                    
                    if (isShiftHeld)
                    {
                        GameObject targetGo = _model.FoundGameObjects[_model.ResultHighlightedIndex];
                        
                        if (targetGo != null)
                        {
                            SceneView view = SceneView.lastActiveSceneView;
                            if (view == null && SceneView.sceneViews.Count > 0) 
                                view = SceneView.sceneViews[0] as SceneView;

                            if (view != null)
                            {
                                view.Focus();
                                Bounds fixedBounds = new Bounds(targetGo.transform.position, Vector3.one * 0.85f);
                                view.Frame(fixedBounds, false);
                            }
                        }
                        
                        _controller.SelectResult(_model.ResultHighlightedIndex, false, false);
                    }
                    else
                    {
                        _controller.SelectResult(_model.ResultHighlightedIndex, isCtrlHeld, isShiftHeld);
                    }
                    e.Use();
                }
            }
            else if (e.keyCode == KeyCode.A && isCtrlHeld)
            {
                if (GUI.GetNameOfFocusedControl() != "ComponentSearchBox")
                {
                    _controller.SelectAllResults();
                    e.Use();
                }
            }
        }
    }
    private void DrawSearchBar()
    {
        GUILayout.BeginHorizontal();

        GUIContent searchIcon = EditorGUIUtility.IconContent("Search Icon");
        GUILayout.Label(searchIcon, GUILayout.Width(20), GUILayout.Height(20));

        Rect searchRect = EditorGUILayout.GetControlRect(false, 20f, GUILayout.ExpandWidth(true));

        if (Event.current.type == EventType.Repaint)
        {
            _model.SearchBarRect = searchRect;
        }

        bool isClickingSearchBox = Event.current.type == EventType.MouseDown && searchRect.Contains(Event.current.mousePosition);

        if (isClickingSearchBox)
        {
            if (string.IsNullOrEmpty(_model.SearchQuery))
            {
                if (_model.CurrentSearchMode == SearchMode.Component)
                    _controller.ShowComponentHistory();
                else if (_model.CurrentSearchMode == SearchMode.Layer)
                    _controller.ShowAllLayers();
                else if (_model.CurrentSearchMode == SearchMode.Tag)
                    _controller.ShowAllTags();

                _window.Repaint();
            }
            else if (!string.IsNullOrEmpty(_model.SearchQuery) && !_model.IsDropdownOpen)
            {
                _model.BackupSearchQuery = _model.SearchQuery;
                _model.KeepResultsOnClear = true;
                _controller.UpdateSearchQuery("");
                _model.KeepResultsOnClear = false;

                // FIXED: Removed _model.NeedsFocus = true; from here.
                // Unity natively focuses the box when you click it. 
                // Forcing it programmatically was interrupting the caret's blink timer!

                _window.Repaint();
            }
        }

        GUI.SetNextControlName("ComponentSearchBox");
        EditorGUI.BeginChangeCheck();

        string newQuery = EditorGUI.TextField(searchRect, GUIContent.none, _model.SearchQuery);

        if (EditorGUI.EndChangeCheck())
        {
            _controller.UpdateSearchQuery(newQuery);
        }

        // THE ULTIMATE CARET FIX
        if (_model.NeedsFocus)
        {
            if (GUI.GetNameOfFocusedControl() == "ComponentSearchBox")
            {
                _model.NeedsFocus = false;
            }
            else
            {
                GUI.FocusControl("ComponentSearchBox");
            }
        }

        GUILayout.EndHorizontal();
    }
    
    
    
    private void DrawTypeSuggestionsOverlay()
    {
        int suggestionCount = 0;
        if (_model.CurrentSearchMode == SearchMode.Component) suggestionCount = _model.FilteredTypes.Count;
        else if (_model.CurrentSearchMode == SearchMode.Layer) suggestionCount = _model.FilteredLayers.Count;
        else if (_model.CurrentSearchMode == SearchMode.Tag) suggestionCount = _model.FilteredTags.Count;

        if (suggestionCount == 0 && !string.IsNullOrWhiteSpace(_model.SearchQuery))
        {
            _model.DropdownRect = new Rect(_model.SearchBarRect.x, _model.SearchBarRect.yMax + 2, _model.SearchBarRect.width, 25);
            EditorGUI.DrawRect(_model.DropdownRect, EditorGUIUtility.isProSkin ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.85f, 0.85f, 0.85f));
            GUI.Box(_model.DropdownRect, "", EditorStyles.helpBox);

            GUIStyle inertStyle = new GUIStyle();
            inertStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.8f, 0.8f, 0.8f) : Color.black;
            inertStyle.alignment = TextAnchor.MiddleLeft;
            inertStyle.padding = new RectOffset(5, 0, 0, 0);

            GUI.Label(_model.DropdownRect, $"No matching {_model.CurrentSearchMode.ToString().ToLower()}s found.", inertStyle);
            return;
        }

        if (suggestionCount > 0)
        {
            int displayCount = Mathf.Min(suggestionCount, 50);
            float rowHeight = 20f;

            float contentHeight = displayCount * rowHeight;
            if (suggestionCount > 50) contentHeight += rowHeight;

            float availableHeight = _window.position.height - _model.SearchBarRect.yMax - 10f;
            float dropdownHeight = Mathf.Min(contentHeight, availableHeight);

            _model.DropdownRect = new Rect(_model.SearchBarRect.x, _model.SearchBarRect.yMax + 2, _model.SearchBarRect.width, dropdownHeight);

            EditorGUI.DrawRect(_model.DropdownRect, EditorGUIUtility.isProSkin ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.85f, 0.85f, 0.85f));
            GUI.Box(_model.DropdownRect, "", EditorStyles.helpBox);

            Rect viewRect = new Rect(0, 0, _model.DropdownRect.width - 16f, contentHeight);
            if (contentHeight <= dropdownHeight) viewRect.width = _model.DropdownRect.width;

            _model.TypesScrollPos = GUI.BeginScrollView(_model.DropdownRect, _model.TypesScrollPos, viewRect);

            if (_model.HighlightedIndex >= suggestionCount)
            {
                _model.HighlightedIndex = Mathf.Max(0, suggestionCount - 1);
            }

            for (int i = 0; i < displayCount; i++)
            {
                if (_model.CurrentSearchMode == SearchMode.Component && i >= _model.FilteredTypes.Count) break;
                if (_model.CurrentSearchMode == SearchMode.Layer && i >= _model.FilteredLayers.Count) break;
                if (_model.CurrentSearchMode == SearchMode.Tag && i >= _model.FilteredTags.Count) break;

                string displayName = "";
                if (_model.CurrentSearchMode == SearchMode.Component) displayName = _model.FilteredTypes[i].FullName ?? _model.FilteredTypes[i].Name;
                else if (_model.CurrentSearchMode == SearchMode.Layer) displayName = _model.FilteredLayers[i];
                else if (_model.CurrentSearchMode == SearchMode.Tag) displayName = _model.FilteredTags[i];

                Rect rowRect = new Rect(0, i * rowHeight, viewRect.width, rowHeight);
                GUIStyle rowStyle = new GUIStyle(EditorStyles.label);
                rowStyle.padding = new RectOffset(5, 5, 2, 2);

                bool isPro = EditorGUIUtility.isProSkin;
                Color kbHighlight = isPro ? new Color(0.17f, 0.36f, 0.53f) : new Color(0.23f, 0.45f, 0.69f);
                Color hoverHighlight = isPro ? new Color(0.25f, 0.25f, 0.25f, 0.5f) : new Color(0.85f, 0.85f, 0.85f, 0.5f);

                bool isMouseOver = rowRect.Contains(Event.current.mousePosition);

                if (i == _model.HighlightedIndex)
                {
                    EditorGUI.DrawRect(rowRect, kbHighlight);
                    rowStyle.normal.textColor = Color.white;
                    if (isMouseOver) EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.1f));
                }
                else if (isMouseOver)
                {
                    EditorGUI.DrawRect(rowRect, hoverHighlight);
                }

                bool showX = _model.CurrentSearchMode == SearchMode.Component && _model.IsShowingHistory;
                Rect xRect = new Rect(rowRect.xMax - 20, rowRect.y + 2, 16, 16);

                if (showX && Event.current.type == EventType.MouseDown && xRect.Contains(Event.current.mousePosition))
                {
                    _controller.RemoveFromHistory(_model.FilteredTypes[i]);
                    Event.current.Use();
                    GUI.FocusControl(null);
                    break;
                }

                if (GUI.Button(rowRect, displayName, rowStyle))
                {
                    if (_model.CurrentSearchMode == SearchMode.Component)
                        _controller.SelectComponentType(_model.FilteredTypes[i]);
                    else if (_model.CurrentSearchMode == SearchMode.Layer)
                        _controller.SelectLayer(_model.FilteredLayers[i]);
                    else if (_model.CurrentSearchMode == SearchMode.Tag)
                        _controller.SelectTag(_model.FilteredTags[i]);

                    GUI.FocusControl(null);
                    break;
                }

                if (showX)
                {
                    EditorGUIUtility.AddCursorRect(xRect, MouseCursor.Link);
                    Texture clearIcon = EditorGUIUtility.IconContent("clear").image;
                    if (clearIcon != null)
                    {
                        GUI.DrawTexture(xRect, clearIcon);
                    }
                    else
                    {
                        GUIStyle fallbackStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
                        GUI.Label(xRect, "X", fallbackStyle);
                    }
                }
            }

            if (suggestionCount > 50)
            {
                Rect moreRect = new Rect(0, displayCount * rowHeight, viewRect.width, rowHeight);
                GUIStyle inertStyle = new GUIStyle();
                inertStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.7f) : Color.black;
                inertStyle.alignment = TextAnchor.MiddleLeft;
                inertStyle.padding = new RectOffset(5, 0, 0, 0);

                GUI.Label(moreRect, $"... and {suggestionCount - 50} more. Keep typing to filter.", inertStyle);
            }

            GUI.EndScrollView();
        }
    }

    private void DrawResults()
    {
        float rowHeight = 20f;
        float totalHeight = _model.FoundGameObjects.Count * rowHeight;

        _model.ObjectsScrollPos = GUILayout.BeginScrollView(_model.ObjectsScrollPos, "box", GUILayout.ExpandHeight(true));

        Rect totalRect = GUILayoutUtility.GetRect(0f, totalHeight, GUILayout.ExpandWidth(true));

        int firstVisible = Mathf.FloorToInt(_model.ObjectsScrollPos.y / rowHeight);
        int lastVisible = Mathf.CeilToInt((_model.ObjectsScrollPos.y + _window.position.height) / rowHeight);

        firstVisible = Mathf.Max(0, firstVisible);
        lastVisible = Mathf.Min(_model.FoundGameObjects.Count - 1, lastVisible);

        for (int i = firstVisible; i <= lastVisible; i++)
        {
            GameObject go = _model.FoundGameObjects[i];
            if (go == null) continue;

            Rect rowRect = new Rect(totalRect.x, totalRect.y + (i * rowHeight), totalRect.width, rowHeight);

            bool isPro = EditorGUIUtility.isProSkin;
            bool isMouseOver = rowRect.Contains(Event.current.mousePosition);
            bool isSelected = _model.SelectedResultIndices.Contains(i);
            bool isKeyboardCursorHere = (i == _model.ResultHighlightedIndex);

            if (isSelected)
            {
                EditorGUI.DrawRect(rowRect, isPro ? new Color(0.17f, 0.36f, 0.53f) : new Color(0.23f, 0.45f, 0.69f));
                if (isKeyboardCursorHere) EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.15f));
            }
            else if (isKeyboardCursorHere)
            {
                EditorGUI.DrawRect(rowRect, isPro ? new Color(0.3f, 0.3f, 0.3f, 0.8f) : new Color(0.8f, 0.8f, 0.8f, 0.8f));
            }
            else if (isMouseOver)
            {
                EditorGUI.DrawRect(rowRect, isPro ? new Color(0.25f, 0.25f, 0.25f, 0.5f) : new Color(0.85f, 0.85f, 0.85f, 0.5f));
            }

            Rect objFieldRect = new Rect(rowRect.x + 2, rowRect.y + 2, rowRect.width - 4, rowRect.height - 4);
            Rect pingIconRect = new Rect(objFieldRect.xMax - 20, objFieldRect.y, 20, objFieldRect.height);

            if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
            {
                if (pingIconRect.Contains(Event.current.mousePosition))
                {
                    EditorGUIUtility.PingObject(go);
                }
                else
                {
                    if (Event.current.clickCount == 2)
                    {
                        // OPTIMIZATION: Object was already selected on click 1. 
                        // Skip the redundant Inspector rebuild and jump straight to camera framing.
                        SceneView view = SceneView.lastActiveSceneView;
                        if (view == null && SceneView.sceneViews.Count > 0)
                            view = SceneView.sceneViews[0] as SceneView;

                        if (view != null)
                        {
                            view.Focus();
                            Bounds fixedBounds = new Bounds(go.transform.position, Vector3.one * 0.85f);
                            view.Frame(fixedBounds, false);
                        }
                    }
                    else
                    {
                        // Handle standard single-click selection
                        _controller.SelectResult(i, Event.current.control || Event.current.command, Event.current.shift);
                    }
                }
                Event.current.Use();
            }

            EventType prevEventType = Event.current.type;
            if (rowRect.Contains(Event.current.mousePosition))
            {
                if (prevEventType == EventType.MouseDown || prevEventType == EventType.MouseUp || prevEventType == EventType.MouseDrag)
                {
                    Event.current.type = EventType.Ignore;
                }
            }

            EditorGUI.ObjectField(objFieldRect, go, typeof(GameObject), true);

            if (rowRect.Contains(Event.current.mousePosition))
            {
                Event.current.type = prevEventType;
            }
        }

        GUILayout.EndScrollView();
    }
    
}



// ==========================================
// 4. THE EDITOR WINDOW
// ==========================================
public class ObjectFinderWindow : EditorWindow
{
    private ObjectFinderModel _model;
    private ObjectFinderController _controller;
    private ObjectFinderView _view;

    // Tracks the exact living instance of the window
    private static ObjectFinderWindow _instance;
    private static Type _lastDockPeerType;

    // Tracks if the window has been opened at least once in this session
    private static bool _hasOpenedOnce = false;


    [MenuItem("Tools/Object Finder")] 
    // 3. Add the dedicated Shortcut attribute
    [Shortcut("Tools/Object Finder", KeyCode.F3)]
    public static void ShowWindow()
    {
        var windows = Resources.FindObjectsOfTypeAll<ObjectFinderWindow>();

        // TOGGLE OFF: If window is open, close it and save the current state
        if (windows.Length > 0 && windows[0] != null)
        {
            _lastDockPeerType = windows[0].GetDockPeerType();
            _hasOpenedOnce = true; // We've officially initialized now
            windows[0].Close();
            return;
        }

        // TOGGLE ON: Determine the logic for placement
        if (_lastDockPeerType != null)
        {
            // Scenario 1: Reopening a previously DOCKED window
            _instance = GetWindow<ObjectFinderWindow>("Object Finder", true, _lastDockPeerType);
        }
        else if (!_hasOpenedOnce)
        {
            // Scenario 2: INITIAL OPENING - Force dock next to Project Browser
            Type projectBrowserType = Type.GetType("UnityEditor.ProjectBrowser,UnityEditor.dll");

            // Fallback to Console if Project Browser isn't found
            if (projectBrowserType == null)
                projectBrowserType = Type.GetType("UnityEditor.ConsoleWindow,UnityEditor.dll");

            if (projectBrowserType != null)
            {
                _instance = GetWindow<ObjectFinderWindow>("Object Finder", true, projectBrowserType);
            }
            else
            {
                _instance = GetWindow<ObjectFinderWindow>("Object Finder");
            }

            _hasOpenedOnce = true;
        }
        else
        {
            // Scenario 3: Reopening a previously FLOATING (undocked) window
            // Since _lastDockPeerType is null but we HAVE opened once, keep it undocked
            _instance = GetWindow<ObjectFinderWindow>("Object Finder");
        }

        _instance.minSize = new Vector2(200, 200);
        _instance.Show();
    }

    private Type GetDockPeerType()
    {
        try
        {
            // 1. Get the internal 'm_Parent' which holds the DockArea data
            var parentField = typeof(EditorWindow).GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic);
            if (parentField == null) return null;

            object parent = parentField.GetValue(this);
            // If parent is null or not a DockArea, the window is floating
            if (parent == null || parent.GetType().Name != "DockArea") return null;

            // 2. Get the 'm_Panes' which is the list of tabs in our specific group
            var panesField = parent.GetType().GetField("m_Panes", BindingFlags.Instance | BindingFlags.NonPublic);
            if (panesField == null) return null;

            if (panesField.GetValue(parent) is List<EditorWindow> panes)
            {
                // 3. Loop through the tabs to find the first one that isn't us
                foreach (EditorWindow pane in panes)
                {
                    if (pane != this && pane != null)
                    {
                        return pane.GetType(); // Found the neighbor!
                    }
                }
            }
        }
        catch { }

        return null; // Floating or lonely
    }

    private void OnLostFocus()
    {
        if (_model != null)
        {
            _model.IsDropdownOpen = false;

            if (_model.HasSelection)
            {
                _model.SearchQuery = _model.CurrentSelectionName;
            }

            Repaint();
        }
    }

    private void OnFocus()
    {
        if (_controller != null)
        {
            _controller.CheckTagsAndLayers();
            _controller.RequestDeferredRefresh(); // Catch anything changed while un-focused
        }
    }


    private void OnEnable()
    {
        minSize = new Vector2(200, 200);
        wantsMouseMove = true;

        _model = new ObjectFinderModel();
        _controller = new ObjectFinderController(_model);
        _view = new ObjectFinderView(_model, _controller, this);

        _controller.UpdateSearchQuery("");

        EditorApplication.hierarchyChanged += _controller.MarkSceneCacheDirty;
        EditorApplication.update += _controller.OnEditorUpdate;
        Undo.postprocessModifications += _controller.OnPostprocessModifications;
        Undo.undoRedoPerformed += _controller.RequestDeferredRefresh;
        _controller.OnRepaintRequested += SafeRepaint;

        if (string.IsNullOrEmpty(_model.SearchQuery))
        {
            if (_model.CurrentSearchMode == SearchMode.Component)
                _controller.ShowComponentHistory();
            else if (_model.CurrentSearchMode == SearchMode.Layer)
                _controller.ShowAllLayers();
            else if (_model.CurrentSearchMode == SearchMode.Tag)
                _controller.ShowAllTags();
        }
    }


    private void OnDisable()
    {
        if (_controller != null)
        {
            EditorApplication.hierarchyChanged -= _controller.MarkSceneCacheDirty;
            EditorApplication.update -= _controller.OnEditorUpdate;
            Undo.postprocessModifications -= _controller.OnPostprocessModifications;
            Undo.undoRedoPerformed -= _controller.RequestDeferredRefresh;
            _controller.OnRepaintRequested -= SafeRepaint;
        }

        _model?.SearchCTS?.Cancel();
    }


    // NEW METHOD: Safely queues the repaint so it doesn't interrupt Unity's internal GUI teardown
    private void SafeRepaint()
    {
        EditorApplication.delayCall += () =>
        {
            if (this != null) Repaint();
        };
    }


    private Vector2 lastMousePos;

    private void OnGUI()
    {
        if (_model.NeedsFocus)
        {
            EditorGUI.FocusTextInControl("ComponentSearchBox");
        }

        if (_view != null)
        {
            _view.Draw();
        }

        if (Event.current.type == EventType.MouseMove)
        {
            Vector2 currentMousePos = Event.current.mousePosition;

            if (Vector2.Distance(currentMousePos, lastMousePos) > 1f)
            {
                lastMousePos = currentMousePos;
                Repaint();
            }
        }
    }
}
