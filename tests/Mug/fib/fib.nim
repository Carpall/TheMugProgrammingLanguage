proc fib(n: int32): int32 =
  result = 1
  var prelast: int32 = 0
  var i = 0
  while i >= 0:
    let tmp = result + prelast
    prelast = result
    result = tmp
    dec i

var i = 0.int32

while i <= 40:
  programResult += fib(i)
  inc i
