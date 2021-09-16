from grammar import Token
from syntax.ast.nodes import *
from compilation import Component

BUILT_INS = '''
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
'''

# FUNCTION_PRE = 'let $ret = $VOID;'

class Emitter(Component):
  def __init__(self, parser):
    super().__init__(parser.filename)
    self.parser = parser
    self.cur = None
    self.indent = ''
  
  def reached_eof(self):
    return self.parser.reached_eof()

  def inc_indent(self):
    self.indent += '  '
  
  def dec_indent(self):
    self.indent = self.indent[:-2]

  def eval_statement(self, statement):
    result = None
    
    if isinstance(statement, (ReturnNode, ScopeReturnNode)):
      result = f'return {self.eval_expr(statement.expr)}'
    elif isinstance(statement, VariableNode):
      result = self.eval_variable_node(statement)
    elif isinstance(statement, ConditionNode):
      result = self.eval_condition_node(statement, is_expr = False)
    elif isinstance(statement, AssignmentNode):
      result = f'{self.eval_expr(statement.left)} {statement.op} {self.eval_expr(statement.expr)}'
    elif isinstance(statement, WhileNode):
      result = f'while ({self.eval_expr(statement.expr)}) {self.eval_block(statement.body)}'
    else:
      result = self.eval_expr(statement)
    
    return result

  def eval_variable_node(self, statement):
    return f'{"const" if not statement.mut else "let"} {statement.name} = {self.eval_expr(statement.expr)}'

  def eval_block(self, block):
    self.inc_indent()
    
    result = '{\n'
    
    for statement in block.statements:
      result += f'{self.indent}{self.eval_statement(statement)};\n'

    self.dec_indent()
    return result + f'{self.indent}}}'

  def eval_function_node(self, node):
    self.cur = node
    
    node.body.statements.append(ReturnNode(Token('ident', '$VOID', None), None))
    return f'function({node.parameters_str()}) {self.eval_block(node.body)}'

  def eval_builtin_call_node(self, node):
    def error():
      self.report('invalid builtin function', node.name.pos)
  
    if not isinstance(node.name, Token):
      error()
    
    return {
      'println': '$println',
      'panic': '$panic'
    }[node.name.value]

  def eval_call_node(self, node):
    parameters = ''
    
    for parameter in node.parameters:
      parameters += f'{self.eval_expr(parameter)}, '
    
    parameters = parameters[:-2]

    return (
      self.eval_builtin_call_node(node) if node.builtin else self.eval_expr(node.name)
    ) + f'({parameters})'

  def eval_condition_node(self, node, is_expr):
    pos = node.pos
    result = ''
    
    while node != None:
      expr = self.eval_expr(node.expr)
      body = self.eval_block(node.body)
      if not is_expr:
        result += f'{node.kind}{f" ({expr})" if not node.is_else() else ""} {body} '
      elif not node.is_else():
        result += f'{expr} ? (function() {body})() : '
      else:
        result += f'(function() {body})()'

      if node.is_else():
        if is_expr:
          result[:-2]

        return result[:-1]

      node = node.node
    
    if is_expr:
      self.report("missing 'else' node", pos)
    
    return result

  def eval_struct_node(self, node):
    self.inc_indent()
    
    assignments = ''
    variables = ''

    self.inc_indent()
    for declaration in node.declarations:
      if not declaration.const:
        assignments += f'\n{self.indent}this.{declaration.name.value} = $param.{declaration.name.value};'
      else:
        self.dec_indent()
        variables += f'{"static " if declaration.static else ""}{declaration.name} = {self.eval_expr(declaration.expr)};'
        self.inc_indent()

    self.dec_indent()
    
    result = f'class {{\n{self.indent}constructor($param) {{ {assignments}\n{self.indent}}}\n{self.indent}{variables}\n'
    
    self.dec_indent()
    return result + f'{self.indent}}}'

  def eval_new_node(self, node):
    parameters = ''

    for field, value in node.field_assignments:
      parameters += f'{field}: {self.eval_expr(value)}, '
    
    return f'new {self.eval_expr(node.name)}({{{parameters[:-2]}}})'

  def eval_expr(self, node):
    expr = None
    
    if isinstance(node, FunctionNode):
      expr = self.eval_function_node(node)
    elif isinstance(node, BinNode):
      expr = f'({self.eval_expr(node.left)}{node.op}{self.eval_expr(node.right)})'
    elif isinstance(node, UnaryNode):
      expr = f'{node.sign}({self.eval_expr(node.expr)})'
    elif isinstance(node, CallNode):
      expr = self.eval_call_node(node)
    elif isinstance(node, Token):
      expr = str(node) if node.value != 'self' else 'this'
    elif isinstance(node, ConditionNode):
      expr = self.eval_condition_node(node, is_expr = True)
    elif isinstance(node, StructNode):
      expr = self.eval_struct_node(node)
    elif isinstance(node, NewNode):
      expr = self.eval_new_node(node)
    elif isinstance(node, MemberNode):
      expr = f'{self.eval_expr(node.base)}.{node.member}'
    elif isinstance(node, ArrayNode):
      expr = str(node.expressions)
    elif isinstance(node, RangeNode):
      expr = f'new $range({node.left}, {node.right})'
  
    return expr

  def emit(self):
    variables = ''

    while not self.reached_eof():
      declaration = self.parser.expect_declaration()
      if not declaration.const:
        self.report("unexpected 'let' at top level", declaration.pos)
        
      variables += self.eval_variable_node(declaration) + ';\n'
    
    return BUILT_INS + variables + '\nmain();'