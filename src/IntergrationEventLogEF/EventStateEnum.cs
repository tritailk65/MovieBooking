using System;

namespace IntergrationEventLog;

public enum EventStateEnum
{
    NotPublished = 0,
    InProgress = 1,
    Published = 2,
    PublishedFailed = 3
}
