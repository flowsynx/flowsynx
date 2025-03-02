//using MediatR;
//using FlowSynx.Abstractions;
//using FlowSynx.Data;

//namespace FlowSynx.Core.Features.Connectors.Query.List;

//public class ConnectorListRequest : IRequest<Result<IEnumerable<object>>>
//{
//    public FieldsList? Fields { get; set; }
//    public FilterList? Filter { get; set; }
//    public SortList? Sort { get; set; }
//    public Paging? Paging { get; set; }
//    public bool? CaseSensitive { get; set; } = false;
//}