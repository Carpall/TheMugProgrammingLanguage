
const $range = class {
  constructor(left, right) {
    this.left = left;
    this.right = right;
    this.index = 0;
  }

  iterate = function() {
    return this.index++ <= this.right - this.left;
  };
};

const $panic = function(msg) {
  throw 'error: ' + msg;
};

const $println = function(...params) {
  if (params.length == 0)
    return $VOID;
  
  let f = params[0];
  let p = [];

  for (let i = 1; i < params.length; i++)
    p.push(params[i]);
  
  console.log(f, ...p);
  return $VOID;
};

const $VOID = {
  $Type: 'void',
  $Value: null
};
const main = function() {
  const x = class {
    constructor($param) { 
      this.x = $param.x;
    }
    a = function() {
      this.x = 10;
      return $VOID;
    };
  };
  const y = new x({x: 1});
  y.a();
  return $println(y);
  return $VOID;
};

main();