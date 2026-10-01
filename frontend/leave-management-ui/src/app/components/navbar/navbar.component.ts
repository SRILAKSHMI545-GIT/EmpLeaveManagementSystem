import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <nav class="navbar" *ngIf="authService.currentUser$ | async as user">
      <div class="nav-brand">
        <span class="brand-icon">🌿</span>
        <span class="brand-title">LeaveFlow</span>
        <span class="role-badge" [ngClass]="user.role.toLowerCase()">{{ user.role }}</span>
      </div>

      <div class="nav-links">
        <a routerLink="/employee" routerLinkActive="active" class="nav-item">My Dashboard</a>
        <a *ngIf="authService.isManager" routerLink="/manager" routerLinkActive="active" class="nav-item">
          Manager Portal
        </a>
      </div>

      <div class="nav-user">
        <div class="user-info">
          <span class="user-name">{{ user.name }}</span>
          <span class="user-dept">{{ user.departmentName || 'General Staff' }}</span>
        </div>
        <button (click)="logout()" class="btn-logout" title="Sign out">
          Sign Out
        </button>
      </div>
    </nav>
  `,
  styles: [`
    .navbar {
      background: #ffffff;
      height: 64px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0 24px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.06);
      border-bottom: 1px solid #e1e8ed;
    }
    .nav-brand {
      display: flex;
      align-items: center;
      gap: 10px;
    }
    .brand-icon {
      font-size: 24px;
    }
    .brand-title {
      font-size: 18px;
      font-weight: 700;
      color: #0f172a;
      letter-spacing: -0.5px;
    }
    .role-badge {
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      padding: 2px 8px;
      border-radius: 12px;
    }
    .role-badge.employee { background: #e0f2fe; color: #0369a1; }
    .role-badge.manager { background: #fef3c7; color: #b45309; }
    .role-badge.admin { background: #fce7f3; color: #be185d; }

    .nav-links {
      display: flex;
      gap: 16px;
    }
    .nav-item {
      text-decoration: none;
      color: #475569;
      font-weight: 500;
      font-size: 14px;
      padding: 8px 14px;
      border-radius: 6px;
      transition: all 0.2s;
    }
    .nav-item:hover {
      background: #f1f5f9;
      color: #0f172a;
    }
    .nav-item.active {
      background: #0066cc;
      color: #ffffff;
    }

    .nav-user {
      display: flex;
      align-items: center;
      gap: 16px;
    }
    .user-info {
      display: flex;
      flex-direction: column;
      text-align: right;
    }
    .user-name {
      font-size: 14px;
      font-weight: 600;
      color: #1e293b;
    }
    .user-dept {
      font-size: 11px;
      color: #64748b;
    }
    .btn-logout {
      background: #f8fafc;
      border: 1px solid #cbd5e1;
      padding: 6px 12px;
      border-radius: 6px;
      font-size: 12px;
      font-weight: 600;
      color: #475569;
      cursor: pointer;
      transition: all 0.2s;
    }
    .btn-logout:hover {
      background: #fee2e2;
      color: #b91c1c;
      border-color: #fca5a5;
    }
  `]
})
export class NavbarComponent {
  constructor(public authService: AuthService) {}

  logout(): void {
    this.authService.logout();
  }
}
