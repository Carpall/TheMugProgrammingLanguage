import io

error AllocationErr { CouldNotAllocate }

type Person { name: str, age: u8 }

func Person(name: str, age: u8): AllocationErr!Person {
  if age == 0 { return AllocationErr.CouldNotAllocate }
  return new Person { name: name, age: age }
}

func main(): i32 {
  println("ok")

  return (
    Person("carpal", 0 as u8) catch e {
      new Person { age: (e as u8) + 100 }
    }
  ).age as i32
}
