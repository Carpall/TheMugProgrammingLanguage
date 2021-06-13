@echo off

echo fibgcc:
bench fibgcc
echo fibclang:
bench fibclang
echo fibmugc:
bench fibmugc
echo fibmugllvm:
bench fibmugllvm
echo fibnim:
bench fibnim
echo fibzig:
bench fibzig

@echo on
