using System;
using System.Reflection;

namespace CSLModsCommon.Extension;

public static class ObjectAccessExtensions {
    private const BindingFlags DefaultFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static T GetField<T>(this object obj, string fieldName, BindingFlags flags = DefaultFlags) {
        var field = obj.GetType().GetField(fieldName, flags);
        if (field == null)
            throw new MissingFieldException(obj.GetType().FullName, fieldName);
        return (T)field.GetValue(obj);
    }

    public static void SetField<T>(this object obj, string fieldName, T value, BindingFlags flags = DefaultFlags) {
        var field = obj.GetType().GetField(fieldName, flags);
        if (field == null)
            throw new MissingFieldException(obj.GetType().FullName, fieldName);
        field.SetValue(obj, value);
    }


    public static T GetProperty<T>(this object obj, string propertyName, BindingFlags flags) {
        var prop = obj.GetType().GetProperty(propertyName, flags);
        if (prop == null)
            throw new MissingMemberException(obj.GetType().FullName, propertyName);
        return (T)prop.GetValue(obj, null);
    }

    public static void SetProperty<T>(this object obj, string propertyName, T value, BindingFlags flags) {
        var prop = obj.GetType().GetProperty(propertyName, flags);
        if (prop == null)
            throw new MissingMemberException(obj.GetType().FullName, propertyName);
        prop.SetValue(obj, value, null);
    }


    public static object InvokeMethod(this object obj, string methodName, object[] parameters = null, BindingFlags flags = DefaultFlags) {
        var method = obj.GetType().GetMethod(methodName, flags);
        if (method == null)
            throw new MissingMethodException(obj.GetType().FullName, methodName);
        return method.Invoke(obj, parameters);
    }
}