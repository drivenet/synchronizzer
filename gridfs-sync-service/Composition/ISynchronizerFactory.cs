﻿using GridFSSyncService.Implementation;

namespace GridFSSyncService.Composition
{
    internal interface ISynchronizerFactory
    {
        ISynchronizer Create(SyncJob job);
    }
}
