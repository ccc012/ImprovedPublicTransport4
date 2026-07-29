namespace CSLModsCommon.UI; 
public delegate void UIElementEventHandler<Element, EventArg>(Element element, EventArg arg);

public delegate void UIElementEventHandler<Element, EventArg1, EventArg2>(Element element, EventArg1 arg, EventArg2 arg2);

public delegate void UIElementEventHandler<EventArg>(EventArg arg);

public delegate void UIElementEventHandler();