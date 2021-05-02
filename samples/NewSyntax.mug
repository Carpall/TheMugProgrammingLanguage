type A:
  a: i32

type B:
  b: i64

type AB = ( A | B )

enum ABKind u8:
  FirstKind
  SecondKind: 11

[import: "__add__", header: "dir/math.h"]
func cadd(a: i64, b: i64): i64

[inline]
func add(a: i32, b: i32) i32: a + b

func main() i32:
  const a = 10
  const b = 10
  return add(a, b)
  
func some():
  const abc = "abc"
  var xyz = 10f

enum MyErr str:
  SomeErr: "some err"
  OtherErr: "other err"

func do(a: i32) MyErr!void:
  return
    if a == 10: MyErr.SomeErr
    else: MyErr.OtherErr

func get_x() !void:
  const x = do(1) catch e: panic!("errorkind: {}", e);
  try do(1)

func `++`(value: &A): value.a++

func other[T](a: T) T:
  while true:
    if a == 10:
      a++
      continue
      
    break
  
  for i in 0..10:
    println(i)
  
  for i: i32, i < 100, i++:
    println(++i)

  return if a == 10: 10 else: new T { }