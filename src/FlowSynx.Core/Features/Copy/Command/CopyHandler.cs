//using MediatR;
//using Microsoft.Extensions.Logging;
//using EnsureThat;
//using FlowSynx.Abstractions;
//using FlowSynx.Core.Features.Storage.List.Query;
//using FlowSynx.Core.Parers.Norms.Storage;
//using FlowSynx.Plugin.Storage.Abstractions.Options;
//using FlowSynx.Plugin.Storage.Copy;
//using FlowSynx.Plugin.Storage.Services;

//namespace FlowSynx.Core.Features.Storage.Copy.Command;

//internal class CopyHandler : IRequestHandler<CopyRequest, Result<CopyResponse>>
//{
//    private readonly ILogger<ListHandler> _logger;
//    private readonly IStorageService _storageService;
//    private readonly IPluginInstanceParser _pluginInstanceParser;

//    public CopyHandler(ILogger<ListHandler> logger, IStorageService storageService, IPluginInstanceParser pluginInstanceParser)
//    {
//        EnsureArg.IsNotNull(logger, nameof(logger));
//        EnsureArg.IsNotNull(storageService, nameof(storageService));
//        _logger = logger;
//        _storageService = storageService;
//        _pluginInstanceParser = pluginInstanceParser;
//    }

//    public async Task<Result<CopyResponse>> Handle(CopyRequest request, CancellationToken cancellationToken)
//    {
//        try
//        {
//            var sourceStorageNorms = _pluginInstanceParser.Parse(request.SourcePath);
//            var destinationStorageNorms = _pluginInstanceParser.Parse(request.DestinationPath);

//            var searchOptions = new StorageSearchOptions()
//            {
//                Include = request.Include,
//                Exclude = request.Exclude,
//                MinimumAge = request.MinAge,
//                MaximumAge = request.MaxAge,
//                MinimumSize = request.MinSize,
//                MaximumSize = request.MaxSize,
//                CaseSensitive = request.CaseSensitive ?? false,
//                Recurse = request.Recurse ?? false
//            };

//            var copyOptions = new StorageCopyOptions()
//            {
//                ClearDestinationPath = request.ClearDestinationPath,
//                OverWriteData = request.OverWriteData
//            };

//            await _storageService.Copy(sourceStorageNorms, destinationStorageNorms, searchOptions, copyOptions, cancellationToken);
//            return await Result<CopyResponse>.SuccessAsync(Resources.CopyHandlerSuccessfullyCopy);
//        }
//        catch (Exception ex)
//        {
//            return await Result<CopyResponse>.FailAsync(new List<string> { ex.Message });
//        }
//    }
//}