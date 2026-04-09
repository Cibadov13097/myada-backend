# Test Guidelines

## Naming

- Use `MethodName_WhenCondition_ShouldExpectedResult`.
- Keep one behavioral expectation per test.

## Isolation

- Create new mocks in each test.
- Do not share mutable state between tests.
- Prefer explicit Arrange-Act-Assert sections.

## Boundaries

- Unit test `Application` and `Domain` behavior first.
- Mock repositories and external services.
- Do not unit test EF Core queries or `DbContext` behavior directly. Cover those with integration tests.
