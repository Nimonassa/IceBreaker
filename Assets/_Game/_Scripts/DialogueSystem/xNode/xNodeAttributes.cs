using UnityEngine;

namespace XNode {
    /// <summary> Use this on Enum fields to fix zoom-offset and focus-leak bugs in xNode </summary>
    public class xNodeEnumAttribute : PropertyAttribute { }

    /// <summary> Use this on UnityEvent fields to fix zoom-offset and layout bugs in xNode </summary>
    public class xNodeUnityEventAttribute : PropertyAttribute { }
}