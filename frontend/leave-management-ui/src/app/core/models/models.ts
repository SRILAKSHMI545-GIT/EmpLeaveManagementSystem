export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

export interface User {
  id: number;
  name: string;
  email: string;
  role: 'Employee' | 'Manager' | 'Admin';
  departmentId?: number;
  departmentName?: string;
  managerId?: number;
  managerName?: string;
  isActive?: boolean;
}

export interface AuthResponse {
  id: number;
  name: string;
  email: string;
  role: 'Employee' | 'Manager' | 'Admin';
  departmentId?: number;
  departmentName?: string;
  managerId?: number;
  managerName?: string;
  token: string;
  refreshToken: string;
  expiresAt: string;
}

export interface LeaveType {
  id: number;
  name: string;
  description?: string;
  defaultAnnualQuota: number;
  requiresApproval: boolean;
}

export interface LeaveBalance {
  id: number;
  userId: number;
  userName: string;
  userEmail: string;
  leaveTypeId: number;
  leaveTypeName: string;
  year: number;
  totalDays: number;
  usedDays: number;
  remainingDays: number;
  lastUpdatedAt: string;
}

export interface LeaveRequest {
  id: number;
  userId: number;
  userName: string;
  userEmail: string;
  departmentId?: number;
  departmentName?: string;
  leaveTypeId: number;
  leaveTypeName: string;
  startDate: string;
  endDate: string;
  daysRequested: number;
  reason: string;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Cancelled';
  decidedById?: number;
  decidedByName?: string;
  managerComment?: string;
  createdAt: string;
  decidedAt?: string;
}

export interface CreateLeaveRequest {
  leaveTypeId: number;
  startDate: string;
  endDate: string;
  reason: string;
}

export interface LeaveDecision {
  managerComment?: string;
}

export interface TeamCalendarEvent {
  requestId: number;
  userId: number;
  userName: string;
  departmentName: string;
  leaveTypeName: string;
  startDate: string;
  endDate: string;
  daysRequested: number;
  status: string;
}

export interface Department {
  id: number;
  name: string;
  description?: string;
}
