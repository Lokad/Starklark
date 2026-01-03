# Subset of test_suite/testdata/go set coverage.

s = set()
assert_eq(len(s), 0)
assert_(not s)
---
s = set([1, 2, 2, 3])
assert_eq(list(s), [1, 2, 3])
assert_(1 in s)
assert_(4 not in s)
---
a = set([1, 2, 3])
b = set([3, 4])
assert_eq(list(a | b), [1, 2, 3, 4])
assert_eq(list(a & b), [3])
assert_eq(list(a - b), [1, 2])
assert_eq(list(a ^ b), [1, 2, 4])
---
s = set([1, 2])
s |= set([2, 3])
assert_eq(list(s), [1, 2, 3])
s &= set([2, 3])
assert_eq(list(s), [2, 3])
s ^= set([3, 4])
assert_eq(list(s), [2, 4])
s -= set([2])
assert_eq(list(s), [4])
---
s = set([1])
s.add(2)
s.add(2)
assert_eq(list(s), [1, 2])
s.discard(1)
assert_eq(list(s), [2])
s.update([2, 3])
assert_eq(list(s), [2, 3])
assert_eq(list(s.union([3, 4])), [2, 3, 4])
assert_eq(list(s.intersection([3, 5])), [3])
assert_eq(list(s.difference([3])), [2])
assert_eq(list(s.symmetric_difference([2, 5])), [3, 5])
---
s = set([1, 2, 3])
assert_(s.issuperset([2]))
assert_(s.issubset([1, 2, 3, 4]))
assert_(s.isdisjoint([4, 5]))
---
s = set([1])
assert_eq(s.pop(), 1)
assert_eq(len(s), 0)
---
set([[]]) ### (unhashable type: 'list'.)
---
s = set()
s.remove(1) ### (element not found.)
---
s = set()
s.pop() ### (pop from empty set.)
