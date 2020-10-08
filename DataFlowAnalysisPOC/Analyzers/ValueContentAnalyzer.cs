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
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace DataFlowAnalysisPOC.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ValueContentAnalyzer : DiagnosticAnalyzer
    {
        private const string RuleId = "S0003";

        private static readonly DiagnosticDescriptor rule = new DiagnosticDescriptor(RuleId, "ValueContentPOC", "Detected literal values {0} on {1}", "POCs", DiagnosticSeverity.Error, true);

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

            context.RegisterOperationAction(operationContext =>
            {
                var cfg = operationContext.GetControlFlowGraph();

                var valueContentResult = ValueContentAnalysis.TryGetOrComputeResult(cfg,
                    containingMethod,
                    wellKnownTypeProvider,
                    operationContext.Options,
                    rule,
                    operationContext.CancellationToken);

                var values = GetValues(cfg, valueContentResult).ToList();

                foreach (var (abstractValue, operations) in values)
                {
                    var operationsStr = Environment.NewLine + "_ " + string.Join(Environment.NewLine + "- ", operations.Select(op => op.Syntax)) + Environment.NewLine;

                    if (abstractValue.IsLiteralState)
                    {
                        var literalValues = string.Join(", ", abstractValue.LiteralValues.Select(v => v.ToString()));

                        Console.WriteLine($"Literal abstract value(s) \"{literalValues}\" found on: {operationsStr}");
                    }
                    else
                    {
                        Console.WriteLine($"Non literal abstract value was found on: {operationsStr}");
                    }
                }
            }, OperationKind.MethodBody);
        }

        private static IEnumerable<(ValueContentAbstractValue abstractValue, List<IOperation> operations)> GetValues(ControlFlowGraph cfg, DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue> result) =>
            cfg.GetOperations()
               .Where(op => result[op] != null)
               .Select(operation => (abstractValue: result[operation], operation))
               .GroupBy(pair => pair.abstractValue)
               .Select(group => (abstractValue: group.Key, operations: group.Select(pair => pair.operation).Distinct().ToList()));
    }
}
