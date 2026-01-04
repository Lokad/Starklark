# Subset of go suite to cover mutation during iteration.

def mutate_list():
  values = [1, 2, 3]
  for value in values:
    values.append(value)

mutate_list() ### (mutate an iterable for an iterator while iterating|Cannot mutate an iterable during iteration.)
---
def mutate_dict():
  items = {"a": 1, "b": 2}
  for key in items:
    items[key] = items[key] + 1

mutate_dict() ### (mutate an iterable for an iterator while iterating|Cannot mutate an iterable during iteration.)
---
def mutate_set():
  values = set([1, 2, 3])
  for value in values:
    values.add(value + 10)

mutate_set() ### (mutate an iterable for an iterator while iterating|Cannot mutate an iterable during iteration.)
