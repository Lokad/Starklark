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
