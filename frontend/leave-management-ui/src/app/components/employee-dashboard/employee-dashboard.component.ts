import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LeaveService } from '../../core/services/leave.service';
import { AuthService } from '../../core/services/auth.service';
import { LeaveBalance, LeaveRequest, LeaveType } from '../../core/models/models';

@Component({
  selector: 'app-employee-dashboard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="dashboard-container">
      <!-- Welcome Header -->
      <div class="header-section">
        <div class="welcome-text">
          <h1>Employee Dashboard</h1>
          <p>Manage your time-off balances and submit new leave requests.</p>
        </div>
        <button class="btn btn-primary" (click)="openApplyModal()">
          + Apply For Leave
        </button>
      </div>

      <!-- Alerts -->
      <div *ngIf="successMessage" class="alert alert-success">
        {{ successMessage }}
      </div>
      <div *ngIf="errorMessage" class="alert alert-danger">
        {{ errorMessage }}
      </div>

      <!-- Leave Balances Section -->
      <div class="section-title">
        <h2>Your Leave Balances ({{ currentYear }})</h2>
      </div>

      <div class="balances-grid">
        <div class="balance-card" *ngFor="let balance of balances" [ngClass]="getBalanceClass(balance)">
          <div class="balance-header">
            <span class="leave-name">{{ balance.leaveTypeName }}</span>
            <span class="quota-badge">{{ balance.totalDays }} Days Total</span>
          </div>
          <div class="balance-body">
            <div class="remaining-count">
              {{ balance.remainingDays }}
              <span class="unit">days left</span>
            </div>
            <div class="progress-bar">
              <div class="progress-fill" [style.width.%]="(balance.usedDays / balance.totalDays) * 100"></div>
            </div>
            <div class="balance-footer">
              <span>Used: <strong>{{ balance.usedDays }}</strong> days</span>
              <span>Available: <strong>{{ balance.remainingDays }}</strong></span>
            </div>
          </div>
        </div>
      </div>

      <!-- Leave Requests History Section -->
      <div class="section-title history-title">
        <h2>My Leave Request History</h2>
      </div>

      <div class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>Leave Type</th>
              <th>Date Range</th>
              <th>Working Days</th>
              <th>Reason</th>
              <th>Submitted At</th>
              <th>Status</th>
              <th>Manager Feedback</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngIf="requests.length === 0">
              <td colspan="8" class="text-center py-4">No leave requests found.</td>
            </tr>
            <tr *ngFor="let req of requests">
              <td><strong>{{ req.leaveTypeName }}</strong></td>
              <td>{{ req.startDate | date:'mediumDate' }} - {{ req.endDate | date:'mediumDate' }}</td>
              <td><span class="days-pill">{{ req.daysRequested }} days</span></td>
              <td class="reason-cell" [title]="req.reason">{{ req.reason }}</td>
              <td>{{ req.createdAt | date:'short' }}</td>
              <td>
                <span class="status-badge" [ngClass]="req.status.toLowerCase()">
                  {{ req.status }}
                </span>
              </td>
              <td>
                <span *ngIf="req.managerComment" class="comment-text">"{{ req.managerComment }}"</span>
                <span *ngIf="!req.managerComment" class="text-muted">—</span>
              </td>
              <td>
                <button
                  *ngIf="req.status === 'Pending' || req.status === 'Approved'"
                  class="btn-cancel"
                  (click)="cancelRequest(req.id)">
                  Cancel
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Apply Leave Modal -->
      <div class="modal-overlay" *ngIf="showApplyModal">
        <div class="modal-card">
          <div class="modal-header">
            <h3>Apply for Leave</h3>
            <button class="close-btn" (click)="closeApplyModal()">&times;</button>
          </div>

          <form [formGroup]="leaveForm" (ngSubmit)="submitLeaveRequest()">
            <div class="modal-body">
              <div *ngIf="modalError" class="alert alert-danger">{{ modalError }}</div>

              <div class="form-group">
                <label for="leaveTypeId">Leave Type *</label>
                <select id="leaveTypeId" formControlName="leaveTypeId" class="form-control" (change)="onLeaveTypeChange()">
                  <option [ngValue]="null" disabled>Select Leave Type</option>
                  <option *ngFor="let lt of leaveTypes" [ngValue]="lt.id">
                    {{ lt.name }} (Quota: {{ lt.defaultAnnualQuota }} days)
                  </option>
                </select>
              </div>

              <!-- Remaining Balance hint for selected type -->
              <div class="balance-hint" *ngIf="selectedBalance">
                Available Balance: <strong>{{ selectedBalance.remainingDays }} days</strong>
              </div>

              <div class="form-row">
                <div class="form-group col">
                  <label for="startDate">Start Date *</label>
                  <input type="date" id="startDate" formControlName="startDate" class="form-control" (change)="calculateRequestedDays()" />
                </div>
                <div class="form-group col">
                  <label for="endDate">End Date *</label>
                  <input type="date" id="endDate" formControlName="endDate" class="form-control" (change)="calculateRequestedDays()" />
                </div>
              </div>

              <div class="estimated-days" *ngIf="estimatedDays > 0">
                Calculated Working Days: <strong>{{ estimatedDays }} days</strong> (excluding weekends)
              </div>

              <div class="form-group">
                <label for="reason">Reason / Purpose *</label>
                <textarea id="reason" formControlName="reason" rows="3" class="form-control" placeholder="Briefly describe the purpose of your leave..."></textarea>
              </div>
            </div>

            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" (click)="closeApplyModal()">Cancel</button>
              <button type="submit" class="btn btn-primary" [disabled]="leaveForm.invalid || submitting">
                <span *ngIf="submitting" class="spinner"></span>
                Submit Request
              </button>
            </div>
          </form>
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
      align-items: center;
      margin-bottom: 24px;
    }
    .welcome-text h1 {
      margin: 0 0 4px 0;
      color: #0f172a;
      font-size: 24px;
    }
    .welcome-text p {
      margin: 0;
      color: #64748b;
      font-size: 14px;
    }
    .btn {
      padding: 10px 18px;
      border-radius: 6px;
      font-weight: 600;
      font-size: 14px;
      border: none;
      cursor: pointer;
      transition: all 0.2s;
    }
    .btn-primary { background: #0066cc; color: #fff; }
    .btn-primary:hover:not(:disabled) { background: #0052a3; }
    .btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }
    .btn-secondary { background: #e2e8f0; color: #334155; }
    .btn-secondary:hover { background: #cbd5e1; }

    .section-title {
      margin-top: 10px;
      margin-bottom: 16px;
    }
    .section-title h2 {
      font-size: 18px;
      color: #1e293b;
      font-weight: 600;
      margin: 0;
    }
    .history-title { margin-top: 36px; }

    .balances-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 16px;
    }
    .balance-card {
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 10px;
      padding: 18px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
      border-left: 4px solid #0066cc;
    }
    .balance-card.annual { border-left-color: #3b82f6; }
    .balance-card.sick { border-left-color: #ef4444; }
    .balance-card.casual { border-left-color: #10b981; }
    .balance-card.maternity { border-left-color: #8b5cf6; }

    .balance-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;
    }
    .leave-name {
      font-weight: 600;
      font-size: 14px;
      color: #1e293b;
    }
    .quota-badge {
      font-size: 11px;
      background: #f1f5f9;
      color: #64748b;
      padding: 2px 6px;
      border-radius: 4px;
    }
    .remaining-count {
      font-size: 28px;
      font-weight: 700;
      color: #0f172a;
      line-height: 1;
      margin-bottom: 8px;
    }
    .remaining-count .unit {
      font-size: 12px;
      font-weight: 400;
      color: #64748b;
    }
    .progress-bar {
      height: 6px;
      background: #e2e8f0;
      border-radius: 3px;
      overflow: hidden;
      margin-bottom: 10px;
    }
    .progress-fill {
      height: 100%;
      background: #0066cc;
      border-radius: 3px;
    }
    .balance-footer {
      display: flex;
      justify-content: space-between;
      font-size: 12px;
      color: #64748b;
    }

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
      max-width: 180px;
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

    .btn-cancel {
      background: #fff;
      border: 1px solid #cbd5e1;
      color: #ef4444;
      font-size: 12px;
      font-weight: 600;
      padding: 4px 8px;
      border-radius: 4px;
      cursor: pointer;
    }
    .btn-cancel:hover {
      background: #fee2e2;
      border-color: #fca5a5;
    }

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
      max-width: 520px;
      box-shadow: 0 10px 25px rgba(0,0,0,0.2);
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
    .modal-body { padding: 20px 24px; }
    .modal-footer {
      padding: 14px 24px;
      border-top: 1px solid #e2e8f0;
      display: flex;
      justify-content: flex-end;
      gap: 12px;
    }
    .form-row { display: flex; gap: 12px; }
    .form-group.col { flex: 1; }
    .form-group { margin-bottom: 16px; }
    .form-group label { display: block; font-size: 13px; font-weight: 600; color: #334155; margin-bottom: 6px; }
    .form-control { width: 100%; box-sizing: border-box; padding: 8px 12px; border: 1px solid #cbd5e1; border-radius: 6px; font-size: 14px; }
    .balance-hint { font-size: 12px; color: #0369a1; background: #e0f2fe; padding: 6px 10px; border-radius: 4px; margin-bottom: 12px; }
    .estimated-days { font-size: 13px; color: #166534; background: #dcfce7; padding: 8px 12px; border-radius: 6px; margin-bottom: 14px; }
    .alert { padding: 10px 14px; border-radius: 6px; font-size: 13px; margin-bottom: 16px; }
    .alert-success { background: #dcfce7; color: #15803d; border: 1px solid #86efac; }
    .alert-danger { background: #fee2e2; color: #b91c1c; border: 1px solid #fca5a5; }
  `]
})
export class EmployeeDashboardComponent implements OnInit {
  balances: LeaveBalance[] = [];
  requests: LeaveRequest[] = [];
  leaveTypes: LeaveType[] = [];
  currentYear = new Date().getFullYear();

  showApplyModal = false;
  leaveForm!: FormGroup;
  submitting = false;
  selectedBalance: LeaveBalance | null = null;
  estimatedDays = 0;

  successMessage = '';
  errorMessage = '';
  modalError = '';

  constructor(
    private leaveService: LeaveService,
    private authService: AuthService,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.loadData();
  }

  initForm(): void {
    this.leaveForm = this.fb.group({
      leaveTypeId: [null, [Validators.required]],
      startDate: ['', [Validators.required]],
      endDate: ['', [Validators.required]],
      reason: ['', [Validators.required, Validators.maxLength(500)]]
    });
  }

  loadData(): void {
    this.leaveService.getMyBalances().subscribe({
      next: res => { if (res.success && res.data) this.balances = res.data; }
    });

    this.leaveService.getMyRequests().subscribe({
      next: res => { if (res.success && res.data) this.requests = res.data; }
    });

    this.leaveService.getLeaveTypes().subscribe({
      next: res => { if (res.success && res.data) this.leaveTypes = res.data; }
    });
  }

  getBalanceClass(b: LeaveBalance): string {
    const name = b.leaveTypeName.toLowerCase();
    if (name.includes('annual')) return 'annual';
    if (name.includes('sick')) return 'sick';
    if (name.includes('casual')) return 'casual';
    if (name.includes('maternity')) return 'maternity';
    return '';
  }

  openApplyModal(): void {
    this.modalError = '';
    this.leaveForm.reset({ leaveTypeId: null, startDate: '', endDate: '', reason: '' });
    this.selectedBalance = null;
    this.estimatedDays = 0;
    this.showApplyModal = true;
  }

  closeApplyModal(): void {
    this.showApplyModal = false;
  }

  onLeaveTypeChange(): void {
    const typeId = this.leaveForm.get('leaveTypeId')?.value;
    this.selectedBalance = this.balances.find(b => b.leaveTypeId === typeId) || null;
  }

  calculateRequestedDays(): void {
    const startStr = this.leaveForm.get('startDate')?.value;
    const endStr = this.leaveForm.get('endDate')?.value;

    if (!startStr || !endStr) {
      this.estimatedDays = 0;
      return;
    }

    const start = new Date(startStr);
    const end = new Date(endStr);

    if (end < start) {
      this.estimatedDays = 0;
      return;
    }

    let count = 0;
    const cur = new Date(start);
    while (cur <= end) {
      const day = cur.getDay();
      if (day !== 0 && day !== 6) { // Not Sunday (0) or Saturday (6)
        count++;
      }
      cur.setDate(cur.getDate() + 1);
    }
    this.estimatedDays = count;
  }

  submitLeaveRequest(): void {
    if (this.leaveForm.invalid) return;

    this.submitting = true;
    this.modalError = '';

    this.leaveService.createLeaveRequest(this.leaveForm.value).subscribe({
      next: res => {
        this.submitting = false;
        if (res.success) {
          this.successMessage = 'Leave request submitted successfully!';
          this.closeApplyModal();
          this.loadData();
          setTimeout(() => this.successMessage = '', 4000);
        }
      },
      error: err => {
        this.submitting = false;
        this.modalError = err.error?.message || (err.error?.errors ? err.error.errors.join(', ') : 'Failed to submit request.');
      }
    });
  }

  cancelRequest(id: number): void {
    if (!confirm('Are you sure you want to cancel this leave request?')) return;

    this.leaveService.cancelRequest(id).subscribe({
      next: res => {
        if (res.success) {
          this.successMessage = 'Leave request cancelled.';
          this.loadData();
          setTimeout(() => this.successMessage = '', 4000);
        }
      },
      error: err => {
        this.errorMessage = err.error?.message || 'Failed to cancel leave request.';
        setTimeout(() => this.errorMessage = '', 4000);
      }
    });
  }
}
