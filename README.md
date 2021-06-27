<img width="200" height="200" src="other/nylonico.png" align="right" />

# Mug is a toy language

<br/><br/><br/><br/>

# Contributing

Ways to contribute:
- Bug report.
- Bug fix.
- Implement feature.

Bug report:
- goto https://github.com/Carpall/mug/issues/new.
- Title: a title as self-explanatory as possible.
- Description: follow template's instructions to exaplain the bug.
- Label: add label 'bug', for bugs, 'feature' for feature request.

Bug fix:
- Report bug, add label "gonna fix".
- Read "Compiler structure explanation" at the end of the current file.
- Find the C# file where make changes.
- Add tests to prove your changes are good enough to be accepted.
- Make a pull request to branch "main".

Implement feature:
- Report feature request by opening an issue, add label "gonna implement".
- Read "Compiler structure explanation" at the end of the current file.
- Find the C# class where make changes.
- Add tests.
- Make a pull request to branch "main".

# Compiler structure explanation:

Compiler is about 10k lines
Optimizer is not implmeneted yet.

You find the compiler's structure at: https://github.com/Carpall/mug/blob/master/other/Current%20Compiler's%20Structure.png

- CompilationUnit contains methods to start a compiler instance.
- CompilationTower contains all compiler components and each compilation unit have a compilation tower, allocated on the heap and reachable from every clas which implements CompilerComponent.

Steps:

- Source is processed by a tokenizer that breaks text into multiple tokens.
- Tokens are processed by a parser that matches token patterns and add nodes to the ast based on the syntax's kind it found.
- TypeInstaller walks the top level declarations and installs functions and types in the symbols table.
- AstSolver solves all types found by the parser (with Parser.ExpectType).
- MIRGenerator generates the MIR code while checks the semantic of the ast nodes.
- MIR is processed by target generator, a class which implements TargetGenerator, that produces any target you want taking sa input MIR code and returning the target in .net object format.
