from typing import Match
from grammar.token import Token
from syntax.ast.nodes import *
from compilation.component import Component
from compilation.pos import Position

def join_with_quotes(tokens):
  result = ''
  for token in tokens:
    result += f"'{token}', "

  return result[:-2]

def eof_token(pos):
  return Token('eof', 'eof', pos)

DEFAULT_VOID_TYPE = Token('ident', 'void', None)
FACTOR_BIN_OP = ['*', '/']
EXPR_BIN_OP = ['+', '-']
UNARY_OP = ['+', '-', '&', '*', '!']
BOOL_BIN_OP = ['==', '!=', '>', '<', '>=', '<=']
CONDITION_OP = ['if', 'elif', 'else']
ASSIGN_OP = ['=', '+=', '-=', '*=']

class Parser(Component):
  def __init__(self, lexer):
    super().__init__(lexer.filename)
    self.lexer = lexer
    self.prev = None
    self.cur = None

    self.fetch()
  
  def reached_eof(self):
    return self.lexer.reached_eof()

  def match(self, kind):
    if not isinstance(kind, list):
      return self.cur.kind == kind

    for knd in kind:
      if self.match(knd):
        return True
    
    return False
    
  def match_advance(self, kind):
    r = self.match(kind)
    if r and len(kind) > 0:
      self.fetch()
    
    return r

  def fetch(self):
    self.prev = self.cur
    self.cur = self.lexer.next()

    if self.cur == None:
      self.cur = eof_token(self.prev.pos)

    return self.cur

  def pos(self):
    return self.cur.pos

  def expect(self, kind):
    if not self.match_advance(kind):
      self.report(f"expected '{kind}', found '{self.cur.value}'", self.pos())

    return self.prev

  def expect_parameter(self, expect_comma):
    if expect_comma:
      self.expect(',')

    name = self.expect('ident')
    type = self.collect_type_notation()

    return name, type

  def collect_function_parameters(self):
    i = 0
    parameters = []
    self.expect('(')
    
    while not self.match_advance(')'):
      parameters.append(self.expect_parameter(i > 0))
      i += 1

    return parameters

  def expect_type(self):
    return self.expect_expr()
  
  def debug(self, title=''):
    print(f'{title} ~ prev: |{self.prev}|, cur: |{self.cur}|')

  def collect_type_notation(self, allow_implicit_void = False):
    if not self.match_advance(':'):
      return DEFAULT_VOID_TYPE if allow_implicit_void else None

    return self.expect_type()

  def expect_block(self):
    statements = []
    pos = self.expect('{').pos

    while not self.match_advance('}'):
      statements.append(self.expect_statement())

    return BlockNode(statements, pos)

  def collect_return_node(self):
    pos = self.prev_pos()
    return ReturnNode(self.expect_expr([';']), pos)

  def collect_while_node(self):
    pos = self.prev_pos()
    return WhileNode(self.expect_expr(), self.expect_block(), pos)

  def expect_statement(self):
    if self.match_advance('return'):
      return self.collect_return_node()
      
    if (var := self.match_variable_node(pub = False, static = False))[0]:
      return var[1]
    
    if self.match_advance('while'):
      return self.collect_while_node()
    
    return self.expect_expr([';'], allow_implicit_return = True)

  def collect_function_node(self):
    pos = self.prev_pos()
    parameters = []
    return_type = DEFAULT_VOID_TYPE

    if self.match('('):
      parameters = self.collect_function_parameters()
      return_type = self.collect_type_notation(True)

    body = self.expect_block()
    
    return FunctionNode(parameters, return_type, body, pos)

  def expect_call(self, name):
    parameters = []
    builtin = self.match_advance('!')
    pos = self.expect('(').pos

    def make_node():
      return CallNode(name, builtin, parameters, pos)

    if self.match_advance(')'):
      return make_node()

    while True:
      parameters.append(self.expect_expr())
      
      if not self.match_advance(','):
        break
    
    self.expect(')')

    return make_node()

  def expect_condition_node(self, first = True):
    if not first and not self.match_advance(CONDITION_OP):
      return None

    kind = self.prev
    expr = None

    if first and kind.kind != 'if':
      self.report('invalid construction', kind.pos)
    
    if kind.kind != 'else':
      expr = self.expect_expr()
    
    body = self.expect_block()

    return ConditionNode(kind, expr, body, self.expect_condition_node(first = False), kind.pos)

  def check_all_scope_returns(self, condition_node):
    pos = condition_node.pos
    nodes = 0
    scope_returns = 0
    while condition_node != None:
      nodes += 1
      scope_returns += 1 if condition_node.body.contains_scope_return() else 0
      condition_node = condition_node.node
    
    if scope_returns > 0 and nodes != scope_returns:
      self.report('not all paths return a value', pos)

  def collect_array_node(self):
    pos = self.prev_pos()
    expressions = []
    
    def make_node():
      return ArrayNode(expressions, pos)

    if self.match_advance(']'):
      return make_node()

    while True:
      expressions.append(self.expect_expr())

      if not self.match_advance(','):
        break
    
    self.expect(']')
    
    return make_node()

  def collect_new_node(self):
    pos = self.prev_pos()
    name = None
    field_assignments = []
    
    if not self.match('{'):
      name = self.expect_expr()
    
    def make_node():
      return NewNode(name, field_assignments, pos)
    
    self.expect('{')

    if self.match_advance('}'):
      return make_node()
    
    while True:
      field = self.expect('ident')
      self.expect(':')
      value = self.expect_expr()
      field_assignments.append((field, value))

      if not self.match_advance(','):
        break
    
    self.expect('}')

    return make_node()

  def expect_term(self, allow_range = True):
    term = None
    sign = None
    
    if self.match_advance(UNARY_OP):
      sign = self.prev

    if self.match_advance(['num', 'str', 'ident']):
      term = self.prev
    elif self.match_advance('fn'):
      term = self.collect_function_node()
    elif self.match_advance('struct'):
      term = self.collect_struct_node()
    elif self.match_advance('enum'):
      term = self.collect_enum_node()
    elif self.match_advance('('):
      term = self.expect_expr([')'])
    elif self.match_advance('if'):
      term = self.expect_condition_node()
      self.check_all_scope_returns(term)
    elif self.match_advance('['):
      term = self.collect_array_node()
    elif self.match_advance('new'):
      term = self.collect_new_node()
    else:
      self.report('expected expression', self.pos())

    if allow_range and self.match_advance('..'):
      pos = self.prev_pos()
      term = RangeNode(term, self.expect_term(allow_range = False), pos)

    while self.match_advance('.'):
      pos = self.prev_pos()
      term = MemberNode(term, self.expect('ident'), pos)
    
    while self.match(['(', '!']):
      pos = self.prev_pos()
      term = self.expect_call(term)

    return term if sign == None else UnaryNode(sign, term, sign.pos)

  def expect_factor(self):
    left = self.expect_term()
    
    while self.match_advance(FACTOR_BIN_OP):
      op = self.prev
      left = BinNode(left, self.expect_term(), op, op.pos)
      
    return left

  def expect_bin(self):
    left = self.expect_factor()
    
    while self.match_advance(EXPR_BIN_OP):
      op = self.prev
      left = BinNode(left, self.expect_factor(), op, op.pos)
      
    return left

  def collect_struct_node(self):
    pos = self.prev_pos()
    self.expect('{')
    
    return StructNode(self.collect_declarations(lambda: self.match_advance('}')), pos)

  def expect_bool_bin(self):
    left = self.expect_bin()
    
    while self.match_advance(BOOL_BIN_OP):
      op = self.prev
      left = BinNode(left, self.expect_bin(), op, op.pos)
      
    return left

  def collect_enum_node(self):
    pos = self.prev_pos()
    members = []

    def make_node():
      return EnumNode(members, pos)

    self.expect('{')

    if self.match_advance('}'):
      return make_node()

    while True:
      members.append(self.expect('ident'))

      if not self.match_advance(','):
        break
    
    self.expect('}')

    return make_node()

  def expect_expr(self, end=[], allow_implicit_return = False):
    node = None
    is_statement = allow_implicit_return

    if (var := self.match_variable_node(pub = False, static = False, just_prototype = True))[0]:
      node = var[1]
    else:
      node = self.expect_bool_bin()

    if is_statement and self.match_advance(ASSIGN_OP):
      op = self.prev
      node = AssignmentNode(node, op, self.expect_expr(), op.pos)

    if len(end) > 0 and not self.match_advance(end):
      if ';' in end and self.match('}') and allow_implicit_return:
        node = ScopeReturnNode(node, node.pos) if not isinstance(node, ConditionNode) or node.body.contains_scope_return() else node
      else:
        self.report(f"missing {join_with_quotes(end)}", self.prev_pos())
    
    return node

  def prev_pos(self):
    return self.prev.pos

  def match_variable_node(self, pub, static, just_prototype = False):
    if not self.match_advance(['let', 'const']):
      return (False, None)
    
    kind = self.prev
    const = kind.value == 'const'
    pos = kind.pos
    mut = kind.kind == 'let' and self.match_advance('mut')
    name = self.expect('ident')
    type = self.collect_type_notation()

    def make_proto():
      return (True, VariableNode(name, const, type, pub, static, mut, None, pos))

    if just_prototype:
      return make_proto()

    if not self.match_advance('='):
      self.expect(';')
      return make_proto()
    
    expr = self.expect_expr([';'])

    node = VariableNode(name, const, type, pub, static, mut, expr, pos)
    return (True, node)

  def expect_node(self, function, params = []):
    r, node = function(*params)
    if not r:
      self.report('unexpected token', self.pos())
    
    return node

  def match_advance_specific(self, value):
    r = (self.cur.kind, self.cur.value) == ('ident', value)
    if r:
      self.fetch()
    
    return r

  def expect_declaration(self):
    return self.expect_node(self.match_variable_node, [self.match_advance_specific('pub'), self.match_advance_specific('static')])

  def collect_declarations(self, cond):
    declarations = []

    while not cond():
      declarations.append(self.expect_declaration())
    
    return declarations

  def parse(self):
    return self.collect_declarations(self.reached_eof)