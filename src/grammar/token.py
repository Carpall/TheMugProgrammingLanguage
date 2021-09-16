class Token:
  def __init__(self, kind, value, pos):
    self.kind = kind
    self.value = value
    self.pos = pos
  
  def __str__(self):
    if self.kind == 'str':
      str = self.value.replace('\\', '\\\\').replace("'", "\\'").replace('\n', '\\n').replace('\t', '\\t')
      return f"'{str}'"
      
    return self.value
  
  def info(self):
    return f"kind: '{self.kind}', value: '{self.value}'"