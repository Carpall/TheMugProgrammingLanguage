func fib(n: i32): i32 {
  return
    if n < 2 { n }
    else { fib(n - 1) + fib(n - 2) }
}

func main(): i32 {
  return fib(40) + *2 i32
}

