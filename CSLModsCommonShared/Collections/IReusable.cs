using System;

namespace CSLModsCommon.Collections;

public interface IReusable : IRentable, IDisposable {
    bool IsDisposed { get; }
    int RentCount { get; }
    void Return();
    void Destroy();
}