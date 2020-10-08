using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace DataFlowAnalysisPOC.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PointsToAnalyzer : DiagnosticAnalyzer
    {
        private const string RuleId = "S0002";

        private static readonly DiagnosticDescriptor rule = new DiagnosticDescriptor(RuleId, "PointsToPOC", "{0} {1} {2}", "POCs", DiagnosticSeverity.Error, true);

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

                // It's weird that the API in the package differs from the API in the repository (PointsToAnalysisKind should exist...)
                var pointsToAnalysisResult = PointsToAnalysis.TryGetOrComputeResult(
                                                cfg,
                                                context.OwningSymbol,
                                                context.Options,
                                                wellKnownTypeProvider,
                                                interproceduralAnalysisConfig,
                                                interproceduralAnalysisPredicateOpt: null);
                if (pointsToAnalysisResult == null)
                {
                    return;
                }
                Dictionary<AbstractLocation, IList<IOperation>> operationsToLocation = MapLocationToListOfOperations(cfg, pointsToAnalysisResult);
                foreach (var loc in operationsToLocation.Keys)
                {
                    if (operationsToLocation[loc].Count > 1)
                    {
                        var operations = operationsToLocation[loc];
                        StringBuilder additionalInfo = ExtractLocationInformation(pointsToAnalysisResult, loc);
                        //var predicateKind = pointsToAnalysisResult.GetPredicateKind(operation);
                        operationContext.ReportDiagnostic(Diagnostic.Create(rule,
                                                                            operations.First().Syntax.GetLocation(),
                                                                            //ToString(pointsToAbstractValue, predicateKind),
                                                                            "MethodBody - ",
                                                                            $"Detected {operations.Count} operations related to",
                                                                            additionalInfo));
                    }
                }

            }, OperationKind.MethodBody);

            context.RegisterOperationAction(operationContext =>
            {
                var cfg = operationContext.GetControlFlowGraph();
                var operation = operationContext.Operation;

                // It's weird that the API in the package differs from the API in the repository (PointsToAnalysisKind should exist...)
                var pointsToAnalysisResult = PointsToAnalysis.TryGetOrComputeResult(
                                                cfg,
                                                context.OwningSymbol,
                                                context.Options,
                                                wellKnownTypeProvider,
                                                interproceduralAnalysisConfig,
                                                interproceduralAnalysisPredicateOpt: null);
                if (pointsToAnalysisResult == null)
                {
                    return;
                }

                var parameter = operation.Descendants().First(x => x.Kind == OperationKind.LocalReference);
                var parameterPointsTo = pointsToAnalysisResult[parameter.Kind, parameter.Syntax];

                foreach (var loc in parameterPointsTo.Locations)
                {
                    StringBuilder additionalInfo = ExtractLocationInformation(pointsToAnalysisResult, loc);
                    operationContext.ReportDiagnostic(Diagnostic.Create(rule,
                                                                        parameter.Syntax.GetLocation(),
                                                                        //ToString(pointsToAbstractValue, predicateKind),
                                                                        $"Invocation {operation.Syntax} parameter {parameter.Syntax} points to ",
                                                                        $"location information: {additionalInfo}",
                                                                        ""));
                }

            }, OperationKind.Invocation);
        }

        private static StringBuilder ExtractLocationInformation(PointsToAnalysisResult pointsToAnalysisResult, AbstractLocation loc)
        {
            var additionalInfo = new StringBuilder();
            if (loc.IsNoLocation)
            {
                additionalInfo.Append("IsNoLocation");
            }
            else
            {
                var locSyntaxNode = loc.TryGetNodeToReportDiagnostic(pointsToAnalysisResult);
                if (loc.IsNull) additionalInfo.Append("Is null");
                if (locSyntaxNode != null) additionalInfo.Append($"syntaxNode {locSyntaxNode}; ");
                if (loc.SymbolOpt != null) additionalInfo.Append($"symbol: {loc.SymbolOpt.Name}; ");
                // from the tests , is seems that the AnalysisEntityOpt is the interesting information
                if (loc.AnalysisEntityOpt != null) additionalInfo.Append($"analysisEnt: {loc.AnalysisEntityOpt.SymbolOpt}; ");
                if (loc.CreationOpt != null) additionalInfo.Append($"creation: {loc.CreationOpt.Syntax}; ");
                if (loc.LocationTypeOpt != null) additionalInfo.Append($"locationType: {loc.LocationTypeOpt}; ");
            }

            return additionalInfo;
        }

        /// <summary>
        /// Iterate over all the operations in the CFG.
        /// Iterate over all the locations for each operations.
        /// Create a mapping between unique Locations and Operations.
        /// </summary>
        private static Dictionary<AbstractLocation, IList<IOperation>> MapLocationToListOfOperations(ControlFlowGraph cfg, PointsToAnalysisResult pointsToAnalysisResult)
        {
            var operationsToLocation = new Dictionary<AbstractLocation, IList<IOperation>>();
            var operationList = GetOperations(cfg).ToList(); // useful to inspect when debugging
            foreach (var operation in operationList)
            {
                var pointsToAbstractValue = pointsToAnalysisResult[operation.Kind, operation.Syntax];

                if (pointsToAbstractValue?.Locations.Count > 0)
                {
                    foreach (var location in pointsToAbstractValue.Locations)
                    {
                        if (!operationsToLocation.ContainsKey(location))
                        {
                            operationsToLocation[location] = new List<IOperation>();
                        }
                        operationsToLocation[location].Add(operation);
                    }
                }
            }

            return operationsToLocation;
        }

        private static IEnumerable<IOperation> GetOperations(ControlFlowGraph controlFlowGraph) =>
            controlFlowGraph.Blocks
                            .SelectMany(block => block.Operations.SelectMany(operation => operation.DescendantsAndSelf()));


    }
}
