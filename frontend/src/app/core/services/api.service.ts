import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Airport } from '../models/airport.model';
import { SearchRequest, SearchResponse } from '../models/flight.model';
import { BookingRequest, BookingResponse } from '../models/booking.model';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getAirports() {
    return this.http.get<Airport[]>(`${this.base}/airports`);
  }

  searchFlights(request: SearchRequest) {
    return this.http.post<SearchResponse>(`${this.base}/flights/search`, request);
  }

  createBooking(request: BookingRequest) {
    return this.http.post<BookingResponse>(`${this.base}/bookings`, request);
  }
}
