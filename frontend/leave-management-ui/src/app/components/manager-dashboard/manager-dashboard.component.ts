import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LeaveService } from '../../core/services/leave.service';
import { AuthService } from '../../core/services/auth.service';
import { LeaveBalance, LeaveRequest, TeamCalendarEvent } from '../../core/models/models';

@Component({
  selector: 'app-manager-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="dashboard-container">
      <!-- Header -->
      <div class="header-section">
        <div>
          <h1>Manager Portal</h1>
          <p>Review team leave requests, track balances, and view schedule calendar.</p>
        </div>
        <div class="tab-switcher">
          <button class="tab-btn" [class.active]="activeTab === 'approvals'" (click)="activeTab = 'approvals'">
            📋 Pending Approvals ({{ pendingRequests.length }})
          </button>
          <button class="tab-btn" [class.active]="activeTab === 'calendar'" (click)="activeTab = 'calendar'">
            📅 Team Calendar
          </button>
          <button class="tab-btn" [class.active]="activeTab === 'balances'" (click)="activeTab = 'balances'">
            👥 Team Balances
          </button>
        </div>
      </div>

      <!-- Alerts -->
      <div *ngIf="successMessage" class="alert alert-success">
        {{ successMessage }}
      </div>
      <div *ngIf="errorMessage" class="alert alert-danger">
        {{ errorMessage }}
      </div>

      <!-- TAB 1: Pending Approvals & All Team Requests -->
      <div *ngIf="activeTab === 'approvals'">
        <div class="section-title">
          <h2>Pending Leave Requests</h2>
        </div>

        <div class="table-container">
          <table class="data-table">
            <thead>
              <tr>
                <th>Employee</th>
                <th>Department</th>
                <th>Leave Type</th>
                <th>Duration</th>
                <th>Days</th>
                <th>Reason</th>
                <th>Requested At</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngIf="pendingRequests.length === 0">
                <td colspan="8" class="text-center py-4">No pending leave requests at this time. 🎉</td>
              </tr>
              <tr *ngFor="let req of pendingRequests">
                <td>
                  <strong>{{ req.userName }}</strong><br>
                  <small class="text-muted">{{ req.userEmail }}</small>
                </td>
                <td>{{ req.departmentName || 'Engineering' }}</td>
                <td><span class="badge badge-leave">{{ req.leaveTypeName }}</span></td>
                <td>{{ req.startDate | date:'mediumDate' }} - {{ req.endDate | date:'mediumDate' }}</td>
                <td><span class="days-pill">{{ req.daysRequested }} days</span></td>
                <td class="reason-cell" [title]="req.reason">{{ req.reason }}</td>
                <td>{{ req.createdAt | date:'short' }}</td>
                <td>
                  <div class="action-buttons">
                    <button class="btn-approve" (click)="openDecisionModal(req, 'Approve')">
                      Approve
                    </button>
                    <button class="btn-reject" (click)="openDecisionModal(req, 'Reject')">
                      Reject
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- All Team History -->
        <div class="section-title history-title">
          <h2>All Team Leave History</h2>
        </div>

        <div class="table-container">
          <table class="data-table">
            <thead>
              <tr>
                <th>Employee</th>
                <th>Leave Type</th>
                <th>Dates</th>
                <th>Days</th>
                <th>Status</th>
                <th>Decision Note</th>
                <th>Reviewed By</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let req of allTeamRequests">
                <td>{{ req.userName }}</td>
                <td>{{ req.leaveTypeName }}</td>
                <td>{{ req.startDate | date:'mediumDate' }} - {{ req.endDate | date:'mediumDate' }}</td>
                <td>{{ req.daysRequested }}</td>
                <td>
                  <span class="status-badge" [ngClass]="req.status.toLowerCase()">
                    {{ req.status }}
                  </span>
                </td>
                <td>{{ req.managerComment || '—' }}</td>
                <td>{{ req.decidedByName || '—' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- TAB 2: Team Calendar Overview -->
      <div *ngIf="activeTab === 'calendar'">
        <div class="section-title">
          <h2>Team Time-Off Schedule & Calendar</h2>
        </div>

        <div class="calendar-list">
          <div *ngIf="calendarEvents.length === 0" class="empty-state">
            No scheduled team leaves in this period.
          </div>
          <div class="calendar-event-card" *ngFor="let ev of calendarEvents" [class.approved]="ev.status === 'Approved'">
            <div class="ev-date">
              <span class="day">{{ ev.startDate | date:'dd' }}</span>
              <span class="month">{{ ev.startDate | date:'MMM' }}</span>
            </div>
            <div class="ev-details">
              <h4>{{ ev.userName }} <span class="dept-tag">({{ ev.departmentName }})</span></h4>
              <p class="ev-type">{{ ev.leaveTypeName }} • <strong>{{ ev.daysRequested }} working days</strong></p>
              <p class="ev-range">{{ ev.startDate | date:'mediumDate' }} - {{ ev.endDate | date:'mediumDate' }}</p>
            </div>
            <div class="ev-status">
              <span class="status-badge" [ngClass]="ev.status.toLowerCase()">{{ ev.status }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- TAB 3: Team Leave Balances -->
      <div *ngIf="activeTab === 'balances'">
        <div class="section-title">
          <h2>Team Members' Leave Balances ({{ currentYear }})</h2>
        </div>

        <div class="table-container">
          <table class="data-table">
            <thead>
              <tr>
                <th>Team Member</th>
                <th>Email</th>
                <th>Leave Type</th>
                <th>Year</th>
                <th>Total Quota</th>
                <th>Used Days</th>
                <th>Remaining Days</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let b of teamBalances">
                <td><strong>{{ b.userName }}</strong></td>
                <td>{{ b.userEmail }}</td>
                <td>{{ b.leaveTypeName }}</td>
                <td>{{ b.year }}</td>
                <td>{{ b.totalDays }} days</td>
                <td><strong class="text-danger">{{ b.usedDays }}</strong> days</td>
                <td><strong class="text-success">{{ b.remainingDays }}</strong> days</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Decision Modal (Approve / Reject) -->
      <div class="modal-overlay" *ngIf="showDecisionModal && selectedRequest">
        <div class="modal-card">
          <div class="modal-header">
            <h3>{{ decisionAction }} Leave Request</h3>
            <button class="close-btn" (click)="closeDecisionModal()">&times;</button>
          </div>
          <div class="modal-body">
            <p>
              Are you sure you want to <strong>{{ decisionAction.toLowerCase() }}</strong> the leave request for
              <strong>{{ selectedRequest.userName }}</strong>?
            </p>
            <div class="req-summary">
              <div><strong>Leave Type:</strong> {{ selectedRequest.leaveTypeName }}</div>
              <div><strong>Duration:</strong> {{ selectedRequest.startDate | date:'mediumDate' }} - {{ selectedRequest.endDate | date:'mediumDate' }} ({{ selectedRequest.daysRequested }} days)</div>
              <div><strong>Reason:</strong> {{ selectedRequest.reason }}</div>
            </div>

            <div class="form-group">
              <label for="comment">Manager Note / Comment (Optional)</label>
              <textarea
                id="comment"
                [(ngModel)]="decisionComment"
                rows="3"
                class="form-control"
                placeholder="Add any instructions or remarks...">
              </textarea>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-secondary" (click)="closeDecisionModal()">Cancel</button>
            <button
              class="btn"
              [ngClass]="decisionAction === 'Approve' ? 'btn-approve-action' : 'btn-reject-action'"
              [disabled]="submittingDecision"
              (click)="submitDecision()">
              <span *ngIf="submittingDecision" class="spinner"></span>
              Confirm {{ decisionAction }}
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 30px 20px;
    }
    .header-section {
      display: flex;
      justify-content: space-between;
      align-items: flex-end;
      margin-bottom: 24px;
      flex-wrap: wrap;
      gap: 16px;
    }
    .header-section h1 { margin: 0 0 4px 0; color: #0f172a; font-size: 24px; }
    .header-section p { margin: 0; color: #64748b; font-size: 14px; }

    .tab-switcher {
      display: flex;
      background: #e2e8f0;
      padding: 4px;
      border-radius: 8px;
      gap: 4px;
    }
    .tab-btn {
      background: transparent;
      border: none;
      padding: 8px 14px;
      border-radius: 6px;
      font-size: 13px;
      font-weight: 600;
      color: #475569;
      cursor: pointer;
      transition: all 0.2s;
    }
    .tab-btn.active {
      background: #ffffff;
      color: #0066cc;
      box-shadow: 0 2px 4px rgba(0,0,0,0.08);
    }

    .section-title h2 {
      font-size: 18px;
      color: #1e293b;
      font-weight: 600;
      margin-bottom: 16px;
    }
    .history-title { margin-top: 36px; }

    .table-container {
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      overflow-x: auto;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
    }
    .data-table {
      width: 100%;
      border-collapse: collapse;
      text-align: left;
      font-size: 13px;
    }
    .data-table th {
      background: #f8fafc;
      padding: 12px 16px;
      font-weight: 600;
      color: #475569;
      border-bottom: 1px solid #e2e8f0;
    }
    .data-table td {
      padding: 14px 16px;
      border-bottom: 1px solid #f1f5f9;
      color: #334155;
    }

    .reason-cell {
      max-width: 160px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .days-pill {
      background: #f0fdf4;
      color: #166534;
      font-weight: 600;
      padding: 2px 8px;
      border-radius: 10px;
      font-size: 12px;
    }
    .badge-leave {
      background: #e0f2fe;
      color: #0369a1;
      padding: 4px 8px;
      border-radius: 4px;
      font-weight: 600;
      font-size: 12px;
    }
    .status-badge {
      font-size: 11px;
      font-weight: 700;
      padding: 4px 10px;
      border-radius: 12px;
      text-transform: uppercase;
    }
    .status-badge.pending { background: #fef3c7; color: #b45309; }
    .status-badge.approved { background: #dcfce7; color: #15803d; }
    .status-badge.rejected { background: #fee2e2; color: #b91c1c; }
    .status-badge.cancelled { background: #f1f5f9; color: #64748b; }

    .action-buttons {
      display: flex;
      gap: 6px;
    }
    .btn-approve {
      background: #10b981;
      color: #fff;
      border: none;
      padding: 6px 12px;
      border-radius: 4px;
      font-weight: 600;
      cursor: pointer;
    }
    .btn-approve:hover { background: #059669; }
    .btn-reject {
      background: #ef4444;
      color: #fff;
      border: none;
      padding: 6px 12px;
      border-radius: 4px;
      font-weight: 600;
      cursor: pointer;
    }
    .btn-reject:hover { background: #dc2626; }

    .calendar-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }
    .calendar-event-card {
      display: flex;
      align-items: center;
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-left: 5px solid #f59e0b;
      padding: 16px 20px;
      border-radius: 8px;
      gap: 20px;
    }
    .calendar-event-card.approved { border-left-color: #10b981; }
    .ev-date {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      background: #f1f5f9;
      width: 50px;
      height: 50px;
      border-radius: 8px;
    }
    .ev-date .day { font-size: 18px; font-weight: 700; color: #0f172a; line-height: 1; }
    .ev-date .month { font-size: 11px; text-transform: uppercase; color: #64748b; }
    .ev-details { flex: 1; }
    .ev-details h4 { margin: 0 0 4px 0; font-size: 15px; color: #0f172a; }
    .dept-tag { font-size: 12px; font-weight: 400; color: #64748b; }
    .ev-type { margin: 0 0 2px 0; font-size: 13px; color: #334155; }
    .ev-range { margin: 0; font-size: 12px; color: #64748b; }

    .modal-overlay {
      position: fixed;
      top: 0; left: 0; right: 0; bottom: 0;
      background: rgba(0,0,0,0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      padding: 20px;
    }
    .modal-card {
      background: #fff;
      border-radius: 12px;
      width: 100%;
      max-width: 480px;
    }
    .modal-header {
      padding: 18px 24px;
      border-bottom: 1px solid #e2e8f0;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .modal-header h3 { margin: 0; font-size: 18px; color: #0f172a; }
    .close-btn { background: none; border: none; font-size: 24px; cursor: pointer; color: #64748b; }
    .modal-body { padding: 20px 24px; font-size: 14px; }
    .req-summary {
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      padding: 12px;
      border-radius: 6px;
      margin: 14px 0;
      font-size: 13px;
      display: flex;
      flex-direction: column;
      gap: 6px;
    }
    .modal-footer {
      padding: 14px 24px;
      border-top: 1px solid #e2e8f0;
      display: flex;
      justify-content: flex-end;
      gap: 12px;
    }
    .form-group label { display: block; font-size: 13px; font-weight: 600; color: #334155; margin-bottom: 6px; }
    .form-control { width: 100%; box-sizing: border-box; padding: 8px 12px; border: 1px solid #cbd5e1; border-radius: 6px; font-size: 14px; }
    .btn { padding: 8px 16px; border-radius: 6px; font-weight: 600; font-size: 13px; border: none; cursor: pointer; }
    .btn-secondary { background: #e2e8f0; color: #334155; }
    .btn-approve-action { background: #10b981; color: #fff; }
    .btn-reject-action { background: #ef4444; color: #fff; }
    .text-danger { color: #dc2626; }
    .text-success { color: #16a34a; }
    .text-muted { color: #94a3b8; }
    .alert { padding: 10px 14px; border-radius: 6px; font-size: 13px; margin-bottom: 16px; }
    .alert-success { background: #dcfce7; color: #15803d; border: 1px solid #86efac; }
    .alert-danger { background: #fee2e2; color: #b91c1c; border: 1px solid #fca5a5; }
  `]
})
export class ManagerDashboardComponent implements OnInit {
  activeTab: 'approvals' | 'calendar' | 'balances' = 'approvals';

  pendingRequests: LeaveRequest[] = [];
  allTeamRequests: LeaveRequest[] = [];
  calendarEvents: TeamCalendarEvent[] = [];
  teamBalances: LeaveBalance[] = [];
  currentYear = new Date().getFullYear();

  showDecisionModal = false;
  selectedRequest: LeaveRequest | null = null;
  decisionAction: 'Approve' | 'Reject' = 'Approve';
  decisionComment = '';
  submittingDecision = false;

  successMessage = '';
  errorMessage = '';

  constructor(
    private leaveService: LeaveService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    // Load Pending Requests
    this.leaveService.getTeamRequests('Pending').subscribe({
      next: res => { if (res.success && res.data) this.pendingRequests = res.data; }
    });

    // Load All Team Requests
    this.leaveService.getTeamRequests().subscribe({
      next: res => { if (res.success && res.data) this.allTeamRequests = res.data; }
    });

    // Load Calendar
    this.leaveService.getTeamCalendar().subscribe({
      next: res => { if (res.success && res.data) this.calendarEvents = res.data; }
    });

    // Load Team Balances
    this.leaveService.getTeamBalances().subscribe({
      next: res => { if (res.success && res.data) this.teamBalances = res.data; }
    });
  }

  openDecisionModal(req: LeaveRequest, action: 'Approve' | 'Reject'): void {
    this.selectedRequest = req;
    this.decisionAction = action;
    this.decisionComment = '';
    this.showDecisionModal = true;
  }

  closeDecisionModal(): void {
    this.showDecisionModal = false;
    this.selectedRequest = null;
  }

  submitDecision(): void {
    if (!this.selectedRequest) return;

    this.submittingDecision = true;
    const reqId = this.selectedRequest.id;
    const call$ = this.decisionAction === 'Approve'
      ? this.leaveService.approveRequest(reqId, { managerComment: this.decisionComment })
      : this.leaveService.rejectRequest(reqId, { managerComment: this.decisionComment });

    call$.subscribe({
      next: res => {
        this.submittingDecision = false;
        if (res.success) {
          this.successMessage = `Leave request successfully ${this.decisionAction.toLowerCase()}d!`;
          this.closeDecisionModal();
          this.loadData();
          setTimeout(() => this.successMessage = '', 4000);
        }
      },
      error: err => {
        this.submittingDecision = false;
        this.errorMessage = err.error?.message || `Failed to ${this.decisionAction.toLowerCase()} request.`;
        setTimeout(() => this.errorMessage = '', 4000);
      }
    });
  }
}
