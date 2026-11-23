// Every test here shares one Oracle database, and some assert on database-wide counts, so they must not run
// in parallel with each other.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
