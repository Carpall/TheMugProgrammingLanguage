
@[code("
#include <iostream>

extern \"C\" void $c_do() {
  std::cout << \"okkk\\n\";
}

"), extern("$c_do"), ext("cpp")
] func do();

func main() {
  do();
}
