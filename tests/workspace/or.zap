import io

func x(): u1 {
  println("called d ehehhe")
  return false
}

func main(): i32 {
  const y = 10
  const z = ((11 == y) | x()) | false
  return z as i32
}
