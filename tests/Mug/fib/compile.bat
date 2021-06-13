@echo off
echo gcc:
bench gcc -O3 fib.c -o fibgcc.exe
echo clang:
bench clang -O3 fib.c -o fibclang.exe

echo mugc:
bench mug build *mode release *output fibmugc fib.mug *means c
echo mugllvm:
bench mug build *mode release *output fibmugllvm fib.mug *means llvm

echo nim:
bench nim c -d:danger -o:fibnim.exe --hints:off fib.nim

echo zig:
bench zig build-exe fib.zig -O ReleaseFast -femit-bin=fibzig.exe

del fibzig.pdb
rmdir /q /s zig-cache
rem copy ..\ov\a.exe a.exe
@echo on
