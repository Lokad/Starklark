# Subset of java list_slices for slice/stride coverage.

values = [1, 2, 3, 4, 5]
assert_eq(values[::-1], [5, 4, 3, 2, 1])
assert_eq(values[3:1:-1], [4, 3])
assert_eq(values[::-2], [5, 3, 1])
