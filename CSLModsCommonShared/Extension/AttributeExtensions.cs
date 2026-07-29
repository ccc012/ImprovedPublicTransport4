using System;
using System.Collections.Generic;
using System.Reflection;

namespace CSLModsCommon.Extension;

public static class AttributeExtensions {
    public static T GetCustomAttribute<T>(this MemberInfo member, bool inherit = true)
        where T : Attribute {
        if (member == null) return null;

        object[] attrs = member.GetCustomAttributes(typeof(T), inherit);
        return attrs.Length > 0 ? (T)attrs[0] : null;
    }

    public static T GetCustomAttribute<T>(this PropertyInfo property, bool inherit = true)
        where T : Attribute {
        if (property == null) return null;

        object[] attrs = property.GetCustomAttributes(typeof(T), inherit);
        return attrs.Length > 0 ? (T)attrs[0] : null;
    }

    public static T GetCustomAttribute<T>(this FieldInfo field, bool inherit = true)
        where T : Attribute {
        if (field == null) return null;

        object[] attrs = field.GetCustomAttributes(typeof(T), inherit);
        return attrs.Length > 0 ? (T)attrs[0] : null;
    }

    public static IEnumerable<T> GetCustomAttributes<T>(this MemberInfo member, bool inherit = true)
        where T : Attribute {
        if (member == null) yield break;

        foreach (object attr in member.GetCustomAttributes(typeof(T), inherit))
            yield return (T)attr;
    }

    public static bool IsDefined<T>(this MemberInfo member, bool inherit = true)
        where T : Attribute {
        if (member == null) return false;
        return member.IsDefined(typeof(T), inherit);
    }

    public static bool TryGetAttribute<T>(this MemberInfo member, out T attribute, bool inherit = true)
        where T : Attribute {
        attribute = member.GetCustomAttribute<T>(inherit);
        return attribute != null;
    }

    public static T GetEnumAttribute<T>(this Enum value)
        where T : Attribute {
        if (value == null) return null;

        Type type = value.GetType();
        string name = Enum.GetName(type, value);
        if (name == null) return null;

        FieldInfo field = type.GetField(name);
        return field?.GetCustomAttribute<T>();
    }

    public static bool TryGetEnumAttribute<T>(this Enum value, out T attribute)
        where T : Attribute {
        attribute = value.GetEnumAttribute<T>();
        return attribute != null;
    }
}