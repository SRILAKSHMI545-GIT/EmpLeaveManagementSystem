import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse,
  CreateLeaveRequest,
  Department,
  LeaveBalance,
  LeaveDecision,
  LeaveRequest,
  LeaveType,
  TeamCalendarEvent,
  User
} from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class LeaveService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // Metadata
  getLeaveTypes(): Observable<ApiResponse<LeaveType[]>> {
    return this.http.get<ApiResponse<LeaveType[]>>(`${this.apiUrl}/metadata/leave-types`);
  }

  getDepartments(): Observable<ApiResponse<Department[]>> {
    return this.http.get<ApiResponse<Department[]>>(`${this.apiUrl}/metadata/departments`);
  }

  // Balances
  getMyBalances(year?: number): Observable<ApiResponse<LeaveBalance[]>> {
    let params = new HttpParams();
    if (year) params = params.set('year', year.toString());
    return this.http.get<ApiResponse<LeaveBalance[]>>(`${this.apiUrl}/leavebalances/my`, { params });
  }

  getTeamBalances(year?: number): Observable<ApiResponse<LeaveBalance[]>> {
    let params = new HttpParams();
    if (year) params = params.set('year', year.toString());
    return this.http.get<ApiResponse<LeaveBalance[]>>(`${this.apiUrl}/leavebalances/team`, { params });
  }

  getAllBalances(year?: number): Observable<ApiResponse<LeaveBalance[]>> {
    let params = new HttpParams();
    if (year) params = params.set('year', year.toString());
    return this.http.get<ApiResponse<LeaveBalance[]>>(`${this.apiUrl}/leavebalances/all`, { params });
  }

  // Leave Requests
  getMyRequests(): Observable<ApiResponse<LeaveRequest[]>> {
    return this.http.get<ApiResponse<LeaveRequest[]>>(`${this.apiUrl}/leave-requests/my`);
  }

  getTeamRequests(status?: string): Observable<ApiResponse<LeaveRequest[]>> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<LeaveRequest[]>>(`${this.apiUrl}/leave-requests/team`, { params });
  }

  getAllRequests(status?: string): Observable<ApiResponse<LeaveRequest[]>> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<LeaveRequest[]>>(`${this.apiUrl}/leave-requests/all`, { params });
  }

  createLeaveRequest(request: CreateLeaveRequest): Observable<ApiResponse<LeaveRequest>> {
    return this.http.post<ApiResponse<LeaveRequest>>(`${this.apiUrl}/leave-requests`, request);
  }

  approveRequest(id: number, decision: LeaveDecision): Observable<ApiResponse<LeaveRequest>> {
    return this.http.put<ApiResponse<LeaveRequest>>(`${this.apiUrl}/leave-requests/${id}/approve`, decision);
  }

  rejectRequest(id: number, decision: LeaveDecision): Observable<ApiResponse<LeaveRequest>> {
    return this.http.put<ApiResponse<LeaveRequest>>(`${this.apiUrl}/leave-requests/${id}/reject`, decision);
  }

  cancelRequest(id: number): Observable<ApiResponse<LeaveRequest>> {
    return this.http.put<ApiResponse<LeaveRequest>>(`${this.apiUrl}/leave-requests/${id}/cancel`, {});
  }

  getTeamCalendar(start?: string, end?: string): Observable<ApiResponse<TeamCalendarEvent[]>> {
    let params = new HttpParams();
    if (start) params = params.set('start', start);
    if (end) params = params.set('end', end);
    return this.http.get<ApiResponse<TeamCalendarEvent[]>>(`${this.apiUrl}/leave-requests/team/calendar`, { params });
  }

  getTeamMembers(): Observable<ApiResponse<User[]>> {
    return this.http.get<ApiResponse<User[]>>(`${this.apiUrl}/auth/team-members`);
  }

  getAllUsers(): Observable<ApiResponse<User[]>> {
    return this.http.get<ApiResponse<User[]>>(`${this.apiUrl}/auth/users`);
  }
}
