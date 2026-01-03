# Subset of test_suite/testdata/go list coverage.

assert_eq([], [])
assert_eq([1, 2], [1, 2])
assert_ne([1, 2], [1, 3])
assert_([0])
assert_(not [])
---
abc = ["a", "b", "c"]
assert_eq(abc[-1], "c")
assert_eq(abc[0], "a")
abc[3] ### (Index out of range)
---
x = [0, 1, 2]
x[1] = 3
x[2] += 4
assert_eq(x, [0, 3, 6])
---
assert_eq([1, 2] + [3, 4], [1, 2, 3, 4])
assert_eq(["a"] * 3, ["a", "a", "a"])
---
items = [1, 2]
items.append(3)
items.extend([4, 5])
items.insert(1, 9)
assert_eq(items, [1, 9, 2, 3, 4, 5])
items.remove(9)
assert_eq(items, [1, 2, 3, 4, 5])
assert_eq(items.pop(), 5)
assert_eq(items, [1, 2, 3, 4])
---
letters = ["b", "a", "n", "a", "n", "a", "s"]
assert_eq(letters.index("a"), 1)
letters.index("z") ### (Value not found in list)
---
assert_eq([2 * x for x in [1, 2, 3]], [2, 4, 6])
assert_eq([x for x in {"a": 1, "b": 2}], ["a", "b"])
assert_eq([(y, x) for x, y in {1: 2, 3: 4}.items()], [(2, 1), (4, 3)])
---
a = [1, 2]
b = [a for a in [3, 4]]
assert_eq(a, [1, 2])
assert_eq(b, [3, 4])
---
def listcompblock():
  c = [1, 2]
  d = [c for c in [3, 4]]
  assert_eq(c, [1, 2])
  assert_eq(d, [3, 4])
listcompblock()
