# Subset of rust int tests for modulo behavior.

int_min = -2147483647 - 1
int_max = 2147483647

assert_eq(0, int_min % -1)
assert_eq(0, int_min % int_min)
assert_eq(2147483646, int_min % int_max)
assert_eq(-1, int_max % int_min)
assert_eq(4, 7 - 2 - 1)
