# Subset of test_suite/testdata/go function coverage.

def f(x=[0]):
  return x
assert_eq(f(), [0])
f().append(1)
assert_eq(f(), [0, 1])
---
def sq(x):
  x[0] += 1
  return x[0] * x[0]
x = [0]
assert_eq(sq(x), 1)
assert_eq(sq(x), 4)
assert_eq(sq(x), 9)
assert_eq(sq(x), 16)
---
def fib(x):
  if x < 2:
    return x
  return fib(x - 2) + fib(x - 1)
fib(10) ### (called recursively)
---
x = 1
def g():
  return x + 1
assert_eq(g(), 2)
---
x = 1
def f():
  x + 1
  x = 3
f() ### (referenced before assignment)
---
def h():
  if False:
    y = 1
  return y
h() ### (referenced before assignment)
