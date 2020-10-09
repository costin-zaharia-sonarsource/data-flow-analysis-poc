# Data flow analysis POC

## Copy Analysis

**CopyToAnalysis** is a dataflow analysis to track `AnalysisEntity` instances that share the same value or reference.

### Examples

```
var x = new MyClass();
object y = x;
```

In this case there will be two `AnalysisEntity` instances, one for `x` and one for `y` and `CopyAnalysis` will compute that variables `x` and `y` have identical `CopyAbstractValue` with `CopyAbstractValueKind.KnownReferenceCopy`. 

```
int c1 = 0;
int c2 = c1;
```

In this case it will compute that `c1` and `c2` have identical `CopyAbstractValue` with `CopyAbstractValueKind.KnownValueCopy`.

### Cons

According to documentation, **CopyAnalysis** is currently off by default for all analyzers as it has known performance issues and needs performance tuning. It can be enabled by end users with **editorconfig** option **copy_analysis**.

## Points-To Analysis (a.k.a. alias analysis or pointer analysis)

PointsToAnalysis: Dataflow analysis to track locations pointed to by AnalysisEntity and IOperation instances. This is the most commonly used dataflow analysis in all our flow based analyzers/analyses. 

- `AnalysisEntity` - an `ISymbol` OR one or more `AbstractIndex` indices to index into the parent entity OR "this" instance OR An allocation or an object creation. Each `AnalysisEntity` has a type and an InstanceLocation.
- `IOperation` - Root type for representing the abstract semantics of C# and VB statements and expressions [source](https://github.com/dotnet/roslyn/blob/version-2.9.0/src/Compilers/Core/Portable/Operations/IOperation.cs). 2.6.1 is the recommended minimum version with first fully supported IOperation release [source](https://github.com/dotnet/roslyn/issues/19014#issuecomment-418149014).

### Example

```
var x = new MyClass();
object y = x;
var z = flag ? new MyClass() : y;
```
**PointsToAnalysis** will compute that variables `x` and `y` have identical non-null `PointsToAbstractValue`, which contains a single `AbstractLocation` corresponding to the first `IObjectCreationOperation` for `new MyClass()`.

Variable `z` has a different `PointsToAbstractValue`, which is guaranteed to be non-null, but has two potential `AbstractLocation`, one for each `IObjectCreationOperation` in the above code.

### Usage details

From my tests, you get a `PointsToAbstractValue` for a given `IOperation` (interesting `IOperations` would be `LocalReference`, `ParameterReference`, `FieldReference`, `PropertyReference` etc).
The `PointsToAbstractValue` has a list of `AbstractLocations`. Inside the locations, the interesting information seems to be in the `AnalysisEntityOpt` which has the `Symbol`; or `CreationOpt` which points to the creation of an object.
It can also tell whether the value is Null.

## Property Set Analysis

Dataflow analysis to track values assigned to one or more properties of an object to identify and flag incorrect/insecure object state.

Currently the API is internal and cannot be used outside `roslyn-analyzers`.

## Value Content Analysis

Dataflow analysis to track possible constant values that might be stored in an AnalysisEntity and IOperation instances. This is identical to constant propagation for constant values stored in non-constant symbols.

Consider the following example:
```
int c1 = 0;
int c2 = 0;
int c3 = c1 + c2;
```

`ValueContentAnalysis` will compute that variables `c1`, `c2` and `c3` have identical `ValueContentAbstractValue` with a single literal value `0`.

Consider the following example:
```
var c = flag == 1 ? ""a"" : ""b"";
var d = c;
```

`ValueContentAnalysis` will compute that `flag == 1 ? "a" : "b"` and variables `d` and `c` have identical `ValueContentAbstractValue` with multiple literal values `"a"` and `"b"`.

## References

[Well-known flow analyses](https://github.com/dotnet/roslyn-analyzers/blob/master/docs/Writing%20dataflow%20analysis%20based%20analyzers.md#well-known-flow-analyses)
