using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace DataFlowAnalysisPOC.Analyzers.Extensions
{
    public static class ControlFlowGraphExtensions
    {
        public static IEnumerable<IOperation> GetOperations(this ControlFlowGraph cfg) =>
            cfg.Blocks
               .SelectMany(block => block.Operations
                                         .SelectMany(operation => operation.DescendantsAndSelf())
                                         .Append(block.BranchValue)
                                         .Where(op => op!= null));
    }
}
