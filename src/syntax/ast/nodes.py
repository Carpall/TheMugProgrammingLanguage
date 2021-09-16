from syntax.ast.node import Node

class VariableNode(Node):
  def __init__(self, name, const, type, pub, static, mut, expr, pos):
    super().__init__(pos)
    self.name = name
    self.const = const
    self.type = type
    self.pub = pub
    self.static = static
    self.mut = mut
    self.expr = expr
  
  def __str__(self):
    def maybe(bool, string):
      return string if bool else ''

    return f'{maybe(self.pub, "pub ")}{maybe(self.static, "static ")}{"const" if self.const else "let"}{maybe(self.mut, " mut")} {self.name.value}{maybe(self.type != None, f": {self.type}")}{maybe(self.expr != None, f" = {self.expr}")}'

class FunctionNode(Node):
  def __init__(self, parameters, return_type, body, pos):
    super().__init__(pos)
    self.parameters = parameters
    self.return_type = return_type
    self.body = body
  
  def parameters_str(self, types = False):
    parameters = ''
    for name, type in self.parameters:
      parameters += f'{name.value}{f": {type}" if types else ""}, '
    
    return parameters[:-2]

  def __str__(self):
    return f'fn({self.parameters_str(types = True)}): {self.return_type} {self.body}'

class ReturnNode(Node):
  def __init__(self, expr, pos):
    super().__init__(pos)
    self.expr = expr
  
  def __str__(self):
    return f'return {self.expr}'

class BinNode(Node):
  def __init__(self, left, right, op, pos):
    super().__init__(pos)
    self.left = left
    self.right = right
    self.op = op
  
  def __str__(self):
    return f'({self.left} {self.op} {self.right})'

class CallNode(Node):
  def __init__(self, name, builtin, parameters, pos):
    super().__init__(pos)
    self.name = name
    self.builtin = builtin
    self.parameters = parameters
  
  def __str__(self):
    parameters = ''
    for parameter in self.parameters:
      parameters += f'{parameter}, '

    return f'{self.name}{"!" if self.builtin else ""}({parameters[:-2]})'

class ConditionNode(Node):
  def __init__(self, kind, expr, body, node, pos):
    super().__init__(pos)
    self.kind = kind
    self.expr = expr
    self.body = body
    self.node = node

  def is_else(self):
    return self.expr == None

  def __str__(self):
    return f'{self.kind}{f" {self.expr}" if self.kind.kind != "else" else ""} {self.body}{f" {self.node}" if self.node != None else ""}'

class ScopeReturnNode(Node):
  def __init__(self, expr, pos):
    super().__init__(pos)
    self.expr = expr
  
  def __str__(self):
    return f'scope_return {self.expr}'

indent = ''

class BlockNode(Node):
  def __init__(self, statements, pos):
    super().__init__(pos)
    self.statements = statements
  
  def contains_scope_return(self):
    for statement in self.statements:
      if isinstance(statement, ConditionNode):
        return statement.body.contains_scope_return()
      
      if isinstance(statement, ScopeReturnNode):
        return True
    
    return False

  def __str__(self):
    global indent
    result = '{\n'
    indent += '  '

    for statement in self.statements:
      result += f'{indent}{statement};\n'
    
    indent = indent[:-2]
    return result + f'{indent}}}'

class StructNode(Node):
  def __init__(self, declarations, pos):
    super().__init__(pos)
    self.declarations = declarations
  
  def __str__(self):
    return f'struct {BlockNode(self.declarations, self.pos)}'

class UnaryNode(Node):
  def __init__(self, sign, expr, pos):
    super().__init__(pos)
    self.sign = sign
    self.expr = expr
  
  def __str__(self):
    return f'{self.sign}({self.expr})'

class ArrayNode(Node):
  def __init__(self, expressions, pos):
    super().__init__(pos)
    self.expressions = expressions
  
  def __str__(self):
    expressions = '['
    for expression in self.expressions:
      expressions += f'{expression}, '

    return expressions[:-2] + ']'

class WhileNode(Node):
  def __init__(self, expr, body, pos):
    super().__init__(pos)
    self.expr = expr
    self.body = body
  
  def __str__(self):
    return f'while {self.expr} {self.body}'

class MemberNode(Node):
  def __init__(self, base, member, pos):
    super().__init__(pos)
    self.base = base
    self.member = member
  
  def __str__(self):
    return f'{self.base}.{self.member}'

class AssignmentNode(Node):
  def __init__(self, left, op, expr, pos):
    super().__init__(pos)
    self.left = left
    self.op = op
    self.expr = expr
  
  def __str__(self):
    return f'{self.left} {self.op} {self.expr}'

class RangeNode(Node):
  def __init__(self, left, right, pos):
    super().__init__(pos)
    self.left = left
    self.right = right
  
  def __str__(self):
    return f'({self.left}..{self.right})'

class EnumNode(Node):
  def __init__(self, members, pos):
    super().__init__(pos)
    self.members = members
  
  def __str__(self):
    members = ''
    for member in self.members:
      members += f'{member}, '

    return f'enum {{ {members[:-2]} }}'

class NewNode(Node):
  def __init__(self, name, field_assignments, pos):
    super().__init__(pos)
    self.name = name
    self.field_assignments = field_assignments
  
  def __str__(self):
    fields = ''
    for field, value in self.field_assignments:
      fields += f'{field}: {value}, '

    return f'new{f" {self.name}" if self.name != None else ""} {{ {fields[:-2]} }}'