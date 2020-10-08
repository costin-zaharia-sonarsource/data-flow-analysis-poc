using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using DataFlowAnalysisPOC.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace DataFlowAnalysisPOC.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CopyAnalyzer : DiagnosticAnalyzer
    {
        private const string RuleId = "S0001";

        private static readonly DiagnosticDescriptor rule = new DiagnosticDescriptor(RuleId, "CopyPOC", "Detected {0} on {1}", "POCs", DiagnosticSeverity.Error, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSymbolStartAction(OnSymbolStart, SymbolKind.Method);

        private static void OnSymbolStart(SymbolStartAnalysisContext context) =>
            context.RegisterOperationBlockStartAction(OnOperationBlockStart);

        private static void OnOperationBlockStart(OperationBlockStartAnalysisContext context)
        {
            if (!(context.OwningSymbol is IMethodSymbol containingMethod))
            {
                return;
            }

            var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(context.Compilation);
            var interproceduralAnalysisConfig = InterproceduralAnalysisConfiguration.Create(context.Options,
                rule,
                containingMethod,
                context.Compilation,
                InterproceduralAnalysisKind.None,
                context.CancellationToken);

            context.RegisterOperationAction(operationContext =>
            {
                var cfg = operationContext.GetControlFlowGraph();

                var copyResults = CopyAnalysis.TryGetOrComputeResult(cfg,
                                                                     context.OwningSymbol,
                                                                     context.Options,
                                                                     wellKnownTypeProvider,
                                                                     interproceduralAnalysisConfig,
                                                                     null);

                foreach (var operation in cfg.GetOperations())
                {
                    var operationCopyResult = copyResults?[operation];
                    if (operationCopyResult?.AnalysisEntities.Count > 1)
                    {
                        operationContext.ReportDiagnostic(Diagnostic.Create(rule,
                                                                            operation.Syntax.GetLocation(),
                                                                            ToString(operationCopyResult),
                                                                            operation.Syntax.ToString()));
                    }
                }

            }, OperationKind.MethodBody);
        }

        private static string ToString(CopyAbstractValue copyAbstractValue)
        {
            var symbols = copyAbstractValue.AnalysisEntities.Select(e => e.SymbolOpt);
            return $"{GetCopyType(copyAbstractValue.Kind)} (symbols: {string.Join(", ", symbols)})";
        }

        private static string GetCopyType(CopyAbstractValueKind kind) =>
            kind switch
            {
                CopyAbstractValueKind.KnownReferenceCopy => "reference copy",
                CopyAbstractValueKind.KnownValueCopy => "value copy",
                _ => throw new NotImplementedException()
            };
    }
}
