import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { LeaveService } from '../../core/services/leave.service';
import { Department, User } from '../../core/models/models';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="auth-container">
      <div class="auth-card">
        <div class="auth-header">
          <div class="logo-icon">🌿</div>
          <h2>Employee Leave Management</h2>
          <p class="subtitle">{{ isRegisterMode ? 'Create a new account' : 'Sign in to access your portal' }}</p>
        </div>

        <!-- Notification Alerts -->
        <div *ngIf="errorMessage" class="alert alert-danger">
          {{ errorMessage }}
        </div>
        <div *ngIf="successMessage" class="alert alert-success">
          {{ successMessage }}
        </div>

        <!-- Quick Demo Switcher -->
        <div class="demo-logins" *ngIf="!isRegisterMode">
          <span class="demo-label">⚡ Quick Fill Demo:</span>
          <div class="demo-buttons">
            <button type="button" class="btn btn-demo" (click)="fillCredentials('employee@company.com', 'Password123!')">
              Employee (John)
            </button>
            <button type="button" class="btn btn-demo" (click)="fillCredentials('manager@company.com', 'Password123!')">
              Manager (Sarah)
            </button>
            <button type="button" class="btn btn-demo" (click)="fillCredentials('admin@company.com', 'Password123!')">
              Admin
            </button>
          </div>
        </div>

        <!-- Login / Register Form -->
        <form [formGroup]="authForm" (ngSubmit)="onSubmit()">
          <!-- Fields for Register Only -->
          <div *ngIf="isRegisterMode" class="form-group">
            <label for="name">Full Name</label>
            <input type="text" id="name" formControlName="name" class="form-control" placeholder="Jane Doe" />
            <div *ngIf="authForm.get('name')?.touched && authForm.get('name')?.invalid" class="field-error">
              Name is required.
            </div>
          </div>

          <div class="form-group">
            <label for="email">Email Address</label>
            <input type="email" id="email" formControlName="email" class="form-control" placeholder="name@company.com" />
            <div *ngIf="authForm.get('email')?.touched && authForm.get('email')?.invalid" class="field-error">
              Valid email is required.
            </div>
          </div>

          <div class="form-group">
            <label for="password">Password</label>
            <input type="password" id="password" formControlName="password" class="form-control" placeholder="••••••••" />
            <div *ngIf="authForm.get('password')?.touched && authForm.get('password')?.invalid" class="field-error">
              Password must be at least 6 characters.
            </div>
          </div>

          <div *ngIf="isRegisterMode">
            <div class="form-group">
              <label for="role">Role</label>
              <select id="role" formControlName="role" class="form-control">
                <option value="Employee">Employee</option>
                <option value="Manager">Manager</option>
                <option value="Admin">Admin</option>
              </select>
            </div>

            <div class="form-group">
              <label for="departmentId">Department</label>
              <select id="departmentId" formControlName="departmentId" class="form-control">
                <option [ngValue]="null">Select Department</option>
                <option *ngFor="let dept of departments" [ngValue]="dept.id">{{ dept.name }}</option>
              </select>
            </div>
          </div>

          <button type="submit" class="btn btn-primary btn-block" [disabled]="loading || authForm.invalid">
            <span *ngIf="loading" class="spinner"></span>
            {{ isRegisterMode ? 'Register Account' : 'Sign In' }}
          </button>
        </form>

        <div class="auth-footer">
          <p>
            {{ isRegisterMode ? 'Already have an account?' : "Don't have an account yet?" }}
            <a href="javascript:void(0)" (click)="toggleMode()">
              {{ isRegisterMode ? 'Sign In' : 'Register Here' }}
            </a>
          </p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-container {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #f0f4f8 0%, #d9e2ec 100%);
      padding: 20px;
    }
    .auth-card {
      background: #ffffff;
      border-radius: 12px;
      box-shadow: 0 10px 25px rgba(0,0,0,0.08);
      width: 100%;
      max-width: 460px;
      padding: 32px;
    }
    .auth-header {
      text-align: center;
      margin-bottom: 24px;
    }
    .logo-icon {
      font-size: 40px;
      margin-bottom: 8px;
    }
    .auth-header h2 {
      margin: 0;
      color: #102a43;
      font-size: 22px;
      font-weight: 700;
    }
    .subtitle {
      color: #627d98;
      font-size: 14px;
      margin-top: 6px;
    }
    .demo-logins {
      background: #f0f4f8;
      border-radius: 8px;
      padding: 10px 12px;
      margin-bottom: 20px;
    }
    .demo-label {
      font-size: 12px;
      font-weight: 600;
      color: #486581;
      display: block;
      margin-bottom: 6px;
    }
    .demo-buttons {
      display: flex;
      gap: 6px;
      flex-wrap: wrap;
    }
    .btn-demo {
      background: #ffffff;
      border: 1px solid #bcccdc;
      color: #243b53;
      font-size: 11px;
      padding: 4px 8px;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 500;
      transition: all 0.2s;
    }
    .btn-demo:hover {
      background: #334e68;
      color: #ffffff;
      border-color: #334e68;
    }
    .form-group {
      margin-bottom: 16px;
    }
    .form-group label {
      display: block;
      font-size: 13px;
      font-weight: 600;
      color: #334e68;
      margin-bottom: 6px;
    }
    .form-control {
      width: 100%;
      box-sizing: border-box;
      padding: 10px 12px;
      border: 1px solid #d9e2ec;
      border-radius: 6px;
      font-size: 14px;
      color: #102a43;
      transition: border-color 0.2s;
    }
    .form-control:focus {
      outline: none;
      border-color: #0066cc;
      box-shadow: 0 0 0 3px rgba(0, 102, 204, 0.15);
    }
    .field-error {
      color: #d32f2f;
      font-size: 12px;
      margin-top: 4px;
    }
    .btn-primary {
      background: #0066cc;
      color: #ffffff;
      border: none;
      padding: 12px;
      border-radius: 6px;
      font-size: 15px;
      font-weight: 600;
      cursor: pointer;
      width: 100%;
      margin-top: 8px;
      transition: background 0.2s;
    }
    .btn-primary:hover:not(:disabled) {
      background: #0052a3;
    }
    .btn-primary:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }
    .alert {
      padding: 10px 14px;
      border-radius: 6px;
      font-size: 13px;
      margin-bottom: 16px;
    }
    .alert-danger {
      background: #ffe3e3;
      color: #c92a2a;
      border: 1px solid #ffa8a8;
    }
    .alert-success {
      background: #e3fafc;
      color: #0c8599;
      border: 1px solid #99e9f2;
    }
    .auth-footer {
      text-align: center;
      margin-top: 20px;
      font-size: 13px;
      color: #627d98;
    }
    .auth-footer a {
      color: #0066cc;
      text-decoration: none;
      font-weight: 600;
    }
    .auth-footer a:hover {
      text-decoration: underline;
    }
  `]
})
export class LoginComponent implements OnInit {
  authForm!: FormGroup;
  isRegisterMode = false;
  loading = false;
  errorMessage = '';
  successMessage = '';
  departments: Department[] = [];

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private leaveService: LeaveService,
    private router: Router
  ) {}

  ngOnInit(): void {
    if (this.authService.isAuthenticated) {
      this.redirectUser();
    }
    this.initForm();
    this.loadDepartments();
  }

  initForm(): void {
    this.authForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      name: [''],
      role: ['Employee'],
      departmentId: [null]
    });
  }

  loadDepartments(): void {
    this.leaveService.getDepartments().subscribe({
      next: res => {
        if (res.success && res.data) {
          this.departments = res.data;
        }
      }
    });
  }

  toggleMode(): void {
    this.isRegisterMode = !this.isRegisterMode;
    this.errorMessage = '';
    this.successMessage = '';

    const nameControl = this.authForm.get('name');
    if (this.isRegisterMode) {
      nameControl?.setValidators([Validators.required]);
    } else {
      nameControl?.clearValidators();
    }
    nameControl?.updateValueAndValidity();
  }

  fillCredentials(email: string, pass: string): void {
    this.authForm.patchValue({ email, password: pass });
  }

  onSubmit(): void {
    if (this.authForm.invalid) return;

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    if (this.isRegisterMode) {
      this.authService.register(this.authForm.value).subscribe({
        next: res => {
          this.loading = false;
          if (res.success) {
            this.redirectUser();
          }
        },
        error: err => {
          this.loading = false;
          this.errorMessage = err.error?.message || (err.error?.errors ? err.error.errors.join(', ') : 'Registration failed');
        }
      });
    } else {
      this.authService.login(this.authForm.value).subscribe({
        next: res => {
          this.loading = false;
          if (res.success) {
            this.redirectUser();
          }
        },
        error: err => {
          this.loading = false;
          this.errorMessage = err.error?.message || 'Invalid email or password';
        }
      });
    }
  }

  private redirectUser(): void {
    if (this.authService.isManager) {
      this.router.navigate(['/manager']);
    } else {
      this.router.navigate(['/employee']);
    }
  }
}
