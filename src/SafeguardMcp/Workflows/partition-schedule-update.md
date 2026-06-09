---
id: partition-schedule-update
name: Update a Partition Password or SSH Key Schedule
description: GET-modify-PUT pattern for CheckSchedules / ChangeSchedules / SshKeyCheckSchedules / SshKeyChangeSchedules with the conditional required-field map keyed on ScheduleType (Never, Hourly, Daily, Weekly, Monthly, Minute) and the exact appliance 400 errors each missing field produces.
tags: partition, schedule, password schedule, check password, change password, ssh key schedule, monthly schedule, weekly schedule, hourly schedule, daily schedule, MonthlyScheduleType, ScheduleType, conditional required fields, RepeatDaysOfWeek, RepeatMonthlyScheduleType
---

ID: partition-schedule-update
Name: Update a Partition Password or SSH Key Schedule
Goal: Modify an existing partition profile schedule (password check, password change, SSH key check, SSH key change) without tripping the ScheduleType-conditional validation errors.

Context: Partition profile schedules live under four parallel endpoint families on each partition:

- `/v4/AssetPartitions/{id}/CheckSchedules`         -- password check schedules
- `/v4/AssetPartitions/{id}/ChangeSchedules`        -- password change schedules
- `/v4/AssetPartitions/{id}/SshKeyCheckSchedules`   -- SSH key check schedules
- `/v4/AssetPartitions/{id}/SshKeyChangeSchedules`  -- SSH key change schedules

All four share the same shape and the same conditional-required-field rules; pick the family that matches what you are scheduling, then follow the same GET-modify-PUT loop.

PUT replaces. You MUST read the schedule body, mutate only the fields you want to change, and PUT the whole thing back. PATCH is not supported on these endpoints.

Steps:

1. List the schedules on the partition to find the one you want to update.
   - `GET /v4/AssetPartitions/{partitionId}/CheckSchedules?fields=Id,Name,ScheduleType,RepeatInterval,StartHour,StartMinute`
   - Note the `Id` of the target schedule.

2. Read the full schedule body. Do NOT use `fields=` here -- PUT replaces, so you need every field.
   - `GET /v4/AssetPartitions/{partitionId}/CheckSchedules/{scheduleId}`
   - Keep the response payload verbatim.

3. Modify the field(s) you want to change. If you change `ScheduleType`, the set of required fields changes too -- see the table below. Either fill in the new required fields or change `ScheduleType` to `Never` first, then PUT, then PUT again with the new shape.

4. PUT the modified body back.
   - `PUT /v4/AssetPartitions/{partitionId}/CheckSchedules/{scheduleId}` with the full body from step 2 plus your edits.

5. Verify with a follow-up GET.
   - `GET /v4/AssetPartitions/{partitionId}/CheckSchedules/{scheduleId}?fields=Id,Name,ScheduleType,...`

Conditional required fields by ScheduleType (live-verified on a current appliance):

The valid `ScheduleType` values are `Never`, `Daily`, `Weekly`, `Monthly`, `Hourly`, `Minute`. There is NO `Manual` value -- sending one yields ApiError 70000 with `"Error converting value \"Manual\" to type 'Pangaea.Data.Transfer.V2.Profile.ScheduleType'"`.

| ScheduleType | What you must set in the body                                                                                                  | Defaults                                                  |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------------- |
| Never        | Just `Name` and `ScheduleType=Never`. `TimeZoneId` is not required.                                                            | All Repeat* and StartHour/StartMinute clear to null/[].   |
| Minute       | `Name`, `ScheduleType=Minute`, `TimeZoneId`. `RepeatInterval` defaults to 1.                                                   | StartHour/StartMinute optional.                           |
| Hourly       | `Name`, `ScheduleType=Hourly`, `TimeZoneId`, `StartMinute`. `RepeatInterval` defaults to 1.                                    | StartHour not applicable.                                 |
| Daily        | `Name`, `ScheduleType=Daily`, `TimeZoneId`, `StartHour`, `StartMinute`. `RepeatInterval` defaults to 1.                        |                                                           |
| Weekly       | `Name`, `ScheduleType=Weekly`, `TimeZoneId`, `StartHour`, `StartMinute`, `RepeatDaysOfWeek` (non-empty array of DayOfWeek).    | `RepeatInterval` defaults to 1 (every N weeks).           |
| Monthly      | `Name`, `ScheduleType=Monthly`, `TimeZoneId`, `StartHour`, `StartMinute`, `RepeatMonthlyScheduleType` -- AND its sub-fields:   |                                                           |
|              | -- `RepeatMonthlyScheduleType=DayOfMonth`        also requires `RepeatDayOfMonth`.                                             |                                                           |
|              | -- `RepeatMonthlyScheduleType=DayOfWeekOfMonth`  also requires `RepeatWeekOfMonth` AND `RepeatDayOfWeek`.                      |                                                           |

The exact 400 errors the appliance returns when one of these is missing (paste this in a bug report; the codes are stable):

| Missing field combination                                                | HTTP | Body                                                                                                                                                              |
| ------------------------------------------------------------------------ | ---- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Weekly without `RepeatDaysOfWeek`                                        | 400  | `{"Code":60350,"Message":"You must specify at least one day of the week for the weekly schedule type for the PasswordCheckSchedule schedule."}`                   |
| Monthly without `RepeatMonthlyScheduleType`                              | 400  | `{"Code":60351,"Message":"You must specify a valid repeat schedule mode for the monthly schedule type for the PasswordCheckSchedule schedule."}`                  |
| Monthly + `DayOfMonth` without `RepeatDayOfMonth`                        | 400  | `{"Code":60352,"Message":"You must specify a day of the month when using repeat by day of the month schedule for the PasswordCheckSchedule schedule."}`           |
| Monthly + `DayOfWeekOfMonth` without `RepeatWeekOfMonth`/`RepeatDayOfWeek` | 400 | `{"Code":60353,"Message":"You must specify the week of the month and day of the week when using the repeat by day of the week of the month schedule for the PasswordCheckSchedule schedule."}` |
| Daily without `StartHour`                                                | 400  | `{"Code":60382,"Message":"You must specify a valid start hour for the PasswordCheckSchedule schedule."}`                                                          |
| Hourly without `StartMinute`                                             | 400  | `{"Code":60383,"Message":"You must specify a valid start minute for the PasswordCheckSchedule schedule."}`                                                        |
| `ScheduleType=Manual` (or any other unknown string)                      | 400  | `{"Code":70000,"Message":"The request is invalid.","ModelState":{"entity.ScheduleType":["Error converting value \"Manual\" to type 'Pangaea.Data.Transfer.V2.Profile.ScheduleType'..."]}}` |

The `PasswordCheckSchedule` token in the message is replaced with the actual schedule type for the endpoint family you are calling (`PasswordChangeSchedule`, `SshKeyCheckSchedule`, `SshKeyChangeSchedule`).

Worked example -- switch a partition's password check schedule to "every Monday and Thursday at 02:30 UTC":

1. `GET /v4/AssetPartitions/57/CheckSchedules/{scheduleId}` -- read the existing body.
2. Modify the body to set:
   ```json
   {
     "Name": "Default Check Schedule",
     "ScheduleType": "Weekly",
     "TimeZoneId": "UTC",
     "StartHour": 2,
     "StartMinute": 30,
     "RepeatInterval": 1,
     "RepeatDaysOfWeek": ["Monday", "Thursday"]
   }
   ```
   (Keep the other fields from the GET response -- the snippet above shows only the changed/required ones.)
3. `PUT /v4/AssetPartitions/57/CheckSchedules/{scheduleId}` with the modified body.
4. `GET /v4/AssetPartitions/57/CheckSchedules/{scheduleId}?fields=Id,Name,ScheduleType,RepeatDaysOfWeek,StartHour,StartMinute` to confirm.

Field cheat sheet:

- `RepeatDaysOfWeek` is an array of `DayOfWeek` strings (`Monday`, `Tuesday`, ...). Must have at least one entry when `ScheduleType=Weekly`.
- `RepeatMonthlyScheduleType` is one of `DayOfMonth` or `DayOfWeekOfMonth`.
- `RepeatDayOfMonth` is 1..31 (and "31" on a 30-day month rolls to the last day -- this is appliance-side, not a client concern).
- `RepeatWeekOfMonth` is `First`, `Second`, `Third`, `Fourth`, or `Last`.
- `RepeatDayOfWeek` (singular, for monthly DayOfWeekOfMonth) is a single `DayOfWeek` string.
- `RepeatInterval` is the "every N units" multiplier and defaults to 1; set it explicitly only when you want every-other-week or every-three-days behavior.
- `TimeZoneId` is required for any `ScheduleType` other than `Never`. The IANA-style id from `GET /v4/Users/me` works here.

Related:
- `password-rotation-status` -- inspect the schedules currently set on a partition before changing them.
- `set-initial-account-password` -- one-time password set workflow that does NOT touch schedules.
- `ssh-key-rotation` -- the SSH-key rotation workflow that uses the SshKey* schedule families this recipe targets.
- `backup-restore` -- backup operations have their own ScheduleType-shaped fields with the same conditional rules.

Source: Live appliance probes against `/v4/AssetPartitions/-1/CheckSchedules` capturing the exact 400 error texts above; ScheduleType enum confirmed against `Pangaea.Data.Transfer.V2.Profile.ScheduleType`.
