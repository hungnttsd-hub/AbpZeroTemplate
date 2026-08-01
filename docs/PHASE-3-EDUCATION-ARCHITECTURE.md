# Phase 3 — Education Architecture

## Database boundary

`IzoneEducation` is a new, standalone SQL Server database. It has its own ABP module tables and an `education` schema; no Store/aquatic entity, migration, or table is retained.

## Initial bounded contexts

| Context | Initial aggregates | Purpose |
| --- | --- | --- |
| Catalog | CourseCategory, CourseLevel, Course, CourseTeacher, CourseBenefit, CourseFaq | Public course catalogue and course details |
| People & CRM | Teacher, Student, Lead | Teacher profiles, authenticated student linkage and consultation capture |
| Training | Campus, CourseClass, Enrollment | Opening schedule, capacity and enrolment records |
| Assessment | PlacementTest, PlacementQuestion, PlacementAttempt, PlacementAnswer | Placement test delivery and scored recommendation |

## Relationship map

```text
CourseCategory / CourseLevel ──> Course <──> Teacher
                                      │
                                      ├──> CourseClass ──> Enrollment <── Student
                                      └──> Lead (interested course)

PlacementTest ──> PlacementQuestion
      │
      └──> PlacementAttempt ──> PlacementAnswer
```

## Permission boundary

The `Education` permission group replaces Store. It scopes Courses, Teachers, Classes, Students, Enrollments, Leads, PlacementTests, and Content. Public reads and lead/test submission are exposed through dedicated application services; privileged management operations require the corresponding backend permission.

## Migration plan

1. `20260801081042_InitialEducation` creates the ABP module tables and all initial `education.*` tables.
2. Future changes must add a new migration; the initial migration is not edited after a shared environment has used it.
3. Connection strings remain environment-owned. Local default uses `IzoneEducation`; production credentials must be supplied outside source control.
