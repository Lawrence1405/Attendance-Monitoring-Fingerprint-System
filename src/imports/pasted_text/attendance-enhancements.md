Modify the **existing Attendance & Supervision Monitoring System only**. Do **not** redesign the existing interface, database structure, navigation, or working features unless explicitly stated below. Preserve all existing functionality while extending the system with the following improvements.

The system should remain clean, professional, and suitable for government office use.

---

# 1. UI & Accessibility

Improve the existing interface without changing its layout.

Requirements:

* Use a color-blind friendly color palette.
* Do not rely solely on gray colors to represent statuses.
* Statuses must be visually distinguishable.
* Preserve the current design language.

### Supervision Status Colors

* 🔴 Red — Supervision period has expired.
* 🟡 Yellow — Supervision will end within one month.
* 🟢 Green — Active supervision.

---

# 2. Client Categories

Maintain separate client categories:

* Probationer
* Parolee
* Pardonee

Requirements:

* Categories must remain logically separated.
* Categories must be searchable.
* Categories must be filterable.
* Existing records must not be merged.

---

# 3. Required Client Information

Each client record must contain at minimum:

* Profile Picture
* Full Name
* Gender
* Category
* Supervising Officer
* CC Number
* Court
* Docket Number
* Supervision Start Date
* Supervision End Date
* DO / NDO
* Remarks

These fields should support searching whenever applicable.

---

# 4. Supervising Officers

Only the following supervising officers should exist:

* Ma'am Maria
* Ma'am Donna
* Sir MJ

Every client must be assigned to exactly one supervising officer.

The system must support:

* Filtering by supervising officer.
* Individual monitoring view for each officer.
* Overall office monitoring.

Display client totals:

* Overall
* Per supervising officer
* Per client category
* Male/Female totals

Example:

> **12 Clients Shown (7 Male, 5 Female)**

---

# 5. Attendance Module

## Fingerprint Attendance

When a fingerprint is recognized:

Display a confirmation dialog showing:

* Client Name
* Attendance Date
* Attendance Time
* Status (Present)

Buttons:

* Confirm
* Cancel

---

## Manual Attendance

If fingerprint recognition fails:

Allow manual attendance.

Requirements:

* Search existing clients only.
* Do not create new clients.
* Search by client name.
* Attendance should be recorded exactly like fingerprint attendance.

---

# 6. Client Details Popup (Enhanced)

When a user clicks or double-clicks a client from any module (Attendance, Dashboard, Monitoring, Search Results, Reports, etc.), open the **Client Details Popup**.

Do **not** replace the current popup. Instead, enhance it with additional sections while keeping the existing summary visible.

---

## Default Summary View

When the popup first opens, display the existing quick information:

* Profile Picture
* Full Name
* Client Category
* Supervising Officer
* Gender
* CC Number
* Court
* Docket Number
* Supervision Status
* Supervision Start Date
* Supervision End Date
* Current Attendance Status
* Remarks Summary

This summary should always be the first thing users see.

---

## Additional Navigation Buttons

Below the summary information, add two buttons:

* **Personal Profile**
* **Case Profile**

These buttons should switch between detailed sections **inside the same popup**.

Do not navigate away from the current page.

---

## Personal Profile

The **Personal Profile** should digitally mirror the official **PPA Form 12**.

Include the following fields:

* PI Number
* Name
* Alias
* Identifying Marks
* Address
* Date of Birth
* Place of Birth
* Age
* Sex
* Civil Status
* Spouse's Name
* Number of Dependents
* Educational Attainment
* Occupation
* Monthly Income
* Hobbies
* Skills
* Religious Affiliation
* Barangay
* Contact Number (if available)

Provide:

* Edit button (authorized users only)

---

## Case Profile

The **Case Profile** should also mirror the official **PPA Form 12**.

Include:

* PSI / PRPD Number
* Criminal Case Number
* Charged With
* Date Committed
* Convicted Of
* Date Convicted
* Sentence
* Court
* Place of Referral
* Date PSI Submitted
* Custody Status
* Date Probation Granted
* Date Probation Order Received
* Period of Probation
* Supervision Start Date
* Supervision End Date
* Date FR/SR/RR/TR Submitted
* Date of TORO
* Date Received
* Investigating Officer
* Supervising Officer

Provide:

* Edit button (authorized users only)

---

## Supervision History

Inside the **Case Profile**, include a supervision history matrix similar to the official paper form.

Display:

* January–December rows
* Year columns
* Monthly attendance
* Historical supervision records

The matrix should automatically populate from attendance records.

It should be **read-only**.

Historical data must never be deleted.

---

## Fingerprint Section

Within the Client Details popup, include a dedicated **Fingerprint** section.

Functions:

* Register Fingerprint
* Update Fingerprint
* Remove Fingerprint
* Enrollment Status
* Date Enrolled

Fingerprint enrollment must **only** exist here.

Do not place enrollment functions inside the Attendance page.

---

## Profile Picture Privacy

Profile pictures should:

Visible:

* Client Details Popup

Hidden:

* Tables
* Attendance Matrix
* Dashboard Lists
* Monitoring Lists

---

# 7. Monthly Attendance Matrix

Each client must maintain a monthly attendance history.

Requirements:

* Linked directly to each client.
* Automatically contributes to:

  * Monthly reports
  * Quarterly reports
  * Yearly reports

When clients become:

* Completed
* Revoked
* Terminated

they must automatically disappear from future attendance lists while keeping historical records available.

---

# 8. Supervision Period Logic

Maintain both:

* Supervision Start Date
* Supervision End Date

Rules:

The Supervision End Date represents the official completion date.

After completion:

* Remove the client from attendance beginning the following month.
* Exclude them from quarterly totals.
* Preserve all historical records.

No attendance should be accepted after supervision officially ends unless explicitly reactivated by an authorized user.

---

# 9. Remarks

Remarks are mandatory.

Each client must support recording:

* Final Report
* Final Report Date
* Termination
* Termination Date
* Violation Report
* Violation Date
* Court Order Disposition
* Date Submitted
* Date Received

Provide filters:

* With Remarks
* Without Remarks

Clients missing remarks should be visually highlighted because they affect audit compliance.

---

# 10. Dashboard

The dashboard should prioritize actionable information rather than only displaying totals.

Display:

* Clients who failed to report
* Clients with pending reports
* Clients missing remarks
* Upcoming supervision expirations
* Expired supervision records

Attendance visibility is more important than simple numeric summaries.

---

# 11. Filtering

Allow multiple filters to work simultaneously.

Supported filters:

### Gender

* Male
* Female

### Status

* DO
* NDO

### Category

* Probationer
* Parolee
* Pardonee

### Supervising Officer

* Ma'am Maria
* Ma'am Donna
* Sir MJ

### Year

### Remarks

* With Remarks
* Without Remarks

### Supervision Status

* Active
* Near Expiration
* Expired
* Completed
* Revoked
* Terminated

Filters should work together without resetting each other.

---

# 12. Historical Records

Changing the selected year must never delete previous records.

Example:

Viewing **2026** should still allow access to **2025** records.

Clients completed in previous years should no longer appear in active attendance for newer years.

---

# 13. Reports & IQPR Alignment

Attendance and Remarks must automatically align with IQPR reporting.

Clients with completed Final Reports should:

* Not be counted as absent.
* Not affect attendance deficiency.
* Be excluded from active monitoring after supervision officially ends.

The system should reduce audit errors caused by incorrect attendance totals.

---

# 14. Navigation

Keep the current navigation.

Only add:

* Sidebar Collapse/Expand button

No other navigation redesign should occur.

---

# 15. Scope

Only enhance the following modules:

* Client Management
* Client Details
* Attendance Monitoring
* Fingerprint Attendance
* Supervision Monitoring
* Dashboard
* Reports
* Remarks
* Filtering
* Historical Records

Do **not** implement unrelated modules.

Do **not** redesign existing pages unless explicitly requested.

---

# Development Constraints

* Preserve the existing UI and workflow unless changes are specifically requested.
* Extend the current database instead of replacing it.
* Maintain backward compatibility with all existing records.
* Ensure historical records are never deleted.
* Keep the interface responsive, simple, and suitable for government office operations.
* The **Client Details Popup** must remain the central location for viewing comprehensive client information, with separate **Personal Profile** and **Case Profile** sections based on the official **PPA Form 12**, while preserving the original quick summary view for efficient daily use.
