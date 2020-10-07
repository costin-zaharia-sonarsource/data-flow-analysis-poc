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

## References

[Well-known flow analyses](https://github.com/dotnet/roslyn-analyzers/blob/master/docs/Writing%20dataflow%20analysis%20based%20analyzers.md#well-known-flow-analyses)
