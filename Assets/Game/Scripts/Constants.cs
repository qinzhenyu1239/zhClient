

public enum EventType { 
    Invalid = -1,
    Internal = 0,  // 规定: 内部的事件[0~10000)
    UI = 10000,
    Net = 20000,
    AI = 30000,
    // ...
}

public enum UIEvent {
    Invalid = -1,
    UIGuestLogin = 1,
    UISelectRole,
}

