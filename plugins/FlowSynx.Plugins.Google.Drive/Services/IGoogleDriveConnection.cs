﻿using FlowSynx.Connectors.Storage.Google.Drive.Models;
using Google.Apis.Drive.v3;

namespace FlowSynx.Connectors.Storage.Google.Drive.Services;

public interface IGoogleDriveConnection
{
    DriveService Connect(GoogleDriveSpecifications specifications);
}