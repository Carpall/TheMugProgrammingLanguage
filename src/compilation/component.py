class CompilationException(Exception):
  def __init__(self, msg, pos = None):
    super().__init__(msg)
    self.pos = pos

class Component:
  def __init__(self, filename):
    self.filename = filename

  def report(self, msg, pos = None):
    raise CompilationException(msg, pos)