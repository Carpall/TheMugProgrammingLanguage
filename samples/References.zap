#[
  References Examples:
    - Only parameters can be passed as refrence
]#

func change(&value: i32) {
  i = 1;
}

func main() {
  var i: i32; # i = 0

  i.change(); # i = 1
}