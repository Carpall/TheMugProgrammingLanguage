func main(): i32 {
  const x = 10
  const y = 10
  const z = 11 == y | y+1 == x | x == 10
  return z as i32
}
