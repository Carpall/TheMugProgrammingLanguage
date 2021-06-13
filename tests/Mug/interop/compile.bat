@echo off
mug build main.mug *output mainc *target obj *means c
mug build main.mug *output mainllvm *target obj *means llvm

gcc add.c -o addgcc.o -c
clang add.c -o addllvm.o -c

gcc mainc.o addgcc.o -o interopc.exe
clang mainllvm.o addllvm.o -o interopllvm.exe
@echo on
