import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

export interface Venue {
  id: number;
  name: string;
  capacity: number;
  city: string;
}

export interface EventItem {
  id: number;
  title: string;
  description: string;
  venueId: number;
  maxCapacity: number;
  startAt: string;
  endAt: string;
  price: number;
  type: string;
  status: string;
}

export interface Reservation {
  id: number;
  eventId: number;
  quantity: number;
  buyerName: string;
  buyerEmail: string;
  status: string;
  reservationCode?: string;
  lostTickets: number;
}

export interface OccupancyReport {
  eventId: number;
  eventTitle: string;
  soldTickets: number;
  lostTickets: number;
  availableTickets: number;
  occupancyPercentage: number;
  income: number;
  status: string;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  getVenues(): Observable<Venue[]> {
    return this.http.get<Venue[]>(`${this.apiUrl}/venues`);
  }

  getEvents(filters: Record<string, string>): Observable<EventItem[]> {
    let params = new HttpParams();
    Object.entries(filters)
      .filter(([, value]) => value)
      .forEach(([key, value]) => {
        params = params.set(key, value);
      });

    return this.http.get<EventItem[]>(`${this.apiUrl}/events`, { params });
  }

  createEvent(payload: unknown): Observable<EventItem> {
    return this.http.post<EventItem>(`${this.apiUrl}/events`, payload);
  }

  createReservation(payload: unknown): Observable<Reservation> {
    return this.http.post<Reservation>(`${this.apiUrl}/reservations`, payload);
  }

  confirmPayment(id: number): Observable<Reservation> {
    return this.http.post<Reservation>(`${this.apiUrl}/reservations/${id}/confirm-payment`, {});
  }

  cancelReservation(id: number): Observable<Reservation> {
    return this.http.post<Reservation>(`${this.apiUrl}/reservations/${id}/cancel`, {});
  }

  getReport(eventId: number): Observable<OccupancyReport> {
    return this.http.get<OccupancyReport>(`${this.apiUrl}/events/${eventId}/occupancy-report`);
  }
}
