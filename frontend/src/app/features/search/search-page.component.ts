import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin, timer } from 'rxjs';
import { finalize, map } from 'rxjs/operators';
import { Airport } from '../../core/models/airport.model';
import { FlightOffer, SearchRequest } from '../../core/models/flight.model';
import { ApiService } from '../../core/services/api.service';

const MIN_LOADING_MS = 2000;

@Component({
  selector: 'app-search-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe, DecimalPipe],
  template: `
    <section class="space-y-6">
      <div class="card">
        <h1 class="text-2xl font-bold text-gray-900 mb-2">Flight Search</h1>
        <p class="text-sm text-gray-500">Find available flights from both providers.</p>

        <form class="mt-6 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4" [formGroup]="searchForm" (ngSubmit)="onSubmit()">
          <div>
            <label class="label" for="originCode">Origin</label>
            <select id="originCode" class="input-field" formControlName="originCode">
              <option value="">Select origin</option>
              @for (airport of airports(); track airport.code) {
                <option [value]="airport.code">
                  {{ airport.code }} - {{ airport.city }} ({{ airport.country }})
                </option>
              }
            </select>
            @if (searchForm.controls.originCode.touched && searchForm.controls.originCode.invalid) {
              <p class="error-text">Origin is required.</p>
            }
          </div>

          <div>
            <label class="label" for="destinationCode">Destination</label>
            <select id="destinationCode" class="input-field" formControlName="destinationCode">
              <option value="">Select destination</option>
              @for (airport of airports(); track airport.code) {
                <option [value]="airport.code">
                  {{ airport.code }} - {{ airport.city }} ({{ airport.country }})
                </option>
              }
            </select>
            @if (searchForm.controls.destinationCode.touched && searchForm.controls.destinationCode.invalid) {
              <p class="error-text">Destination is required.</p>
            }
          </div>

          <div>
            <label class="label" for="departureDate">Departure date</label>
            <input id="departureDate" type="date" class="input-field" formControlName="departureDate" [min]="minDate()" />
            @if (searchForm.controls.departureDate.touched && searchForm.controls.departureDate.invalid) {
              <p class="error-text">Departure date is required.</p>
            }
          </div>

          <div>
            <label class="label" for="passengers">Passengers</label>
            <input id="passengers" type="number" class="input-field" formControlName="passengers" min="1" max="9" />
            @if (searchForm.controls.passengers.touched && searchForm.controls.passengers.invalid) {
              <p class="error-text">Passengers must be between 1 and 9.</p>
            }
          </div>

          <div>
            <label class="label" for="cabinClass">Cabin class</label>
            <select id="cabinClass" class="input-field" formControlName="cabinClass">
              @for (cabin of cabinClasses; track cabin) {
                <option [value]="cabin">{{ cabin }}</option>
              }
            </select>
          </div>

          <div class="md:col-span-2 lg:col-span-3 flex items-end gap-3">
            <button type="submit" class="btn-primary" [disabled]="searchForm.invalid || isLoading()">
              {{ isLoading() ? 'Searching...' : 'Search flights' }}
            </button>
            @if (errorMessage()) {
              <p class="text-sm text-red-600">{{ errorMessage() }}</p>
            }
          </div>
        </form>
      </div>

      @if (isLoading()) {
        <div class="card">
          <div class="flex items-center gap-3 text-sm text-gray-600">
            <span class="inline-block h-4 w-4 animate-spin rounded-full border-2 border-gray-300 border-t-brand-600"></span>
            Searching flights...
          </div>
        </div>
      }

      @if (!isLoading() && sortedOffers().length > 0) {
        <div class="card">
          <div class="mb-4 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <h2 class="text-lg font-semibold text-gray-900">Results</h2>
              <p class="text-sm text-gray-500">{{ sortedOffers().length }} flight(s) found</p>
            </div>
            <div class="w-full sm:w-64">
              <label class="label" for="sortBy">Sort by</label>
              <select id="sortBy" class="input-field" [value]="selectedSort()" (change)="onSortChange($any($event.target).value)">
                @for (option of sortOptions; track option.value) {
                  <option [value]="option.value">{{ option.label }}</option>
                }
              </select>
            </div>
          </div>

          <div class="overflow-x-auto">
            <table class="min-w-full divide-y divide-gray-200">
              <thead class="bg-gray-50">
                <tr class="text-left text-xs font-semibold uppercase tracking-wide text-gray-500">
                  <th class="px-4 py-3">Provider</th>
                  <th class="px-4 py-3">Flight</th>
                  <th class="px-4 py-3">Departure</th>
                  <th class="px-4 py-3">Arrival</th>
                  <th class="px-4 py-3">Duration</th>
                  <th class="px-4 py-3">Cabin</th>
                  <th class="px-4 py-3">Per passenger</th>
                  <th class="px-4 py-3">Total</th>
                  <th class="px-4 py-3">Action</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100 bg-white text-sm text-gray-700">
                @for (offer of sortedOffers(); track offer.id) {
                  <tr>
                    <td class="px-4 py-3 font-medium text-gray-900">{{ offer.provider }}</td>
                    <td class="px-4 py-3">{{ offer.flightNumber }}</td>
                    <td class="px-4 py-3">{{ offer.departureTime | date:'short' }}</td>
                    <td class="px-4 py-3">{{ offer.arrivalTime | date:'short' }}</td>
                    <td class="px-4 py-3">{{ durationLabel(offer.durationMinutes) }}</td>
                    <td class="px-4 py-3">{{ offer.cabinClass }}</td>
                    <td class="px-4 py-3">{{ offer.pricePerPassenger | number:'1.2-2' }} {{ offer.currency }}</td>
                    <td class="px-4 py-3 font-semibold text-gray-900">{{ offer.totalPrice | number:'1.2-2' }} {{ offer.currency }}</td>
                    <td class="px-4 py-3">
                      <button type="button" class="btn-secondary" (click)="goToBooking(offer)">Book</button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }

      @if (!isLoading() && hasSearched() && sortedOffers().length === 0 && !errorMessage()) {
        <div class="card">
          <h2 class="text-lg font-semibold text-gray-900">No flights found</h2>
          <p class="mt-2 text-sm text-gray-600">
            No matching flights were found for this route and date. Try another destination, cabin class, or departure date.
          </p>
        </div>
      }
    </section>
  `,
})
export class SearchPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  readonly sortOptions = [
    { value: 'price-asc', label: 'Price: low to high' },
    { value: 'price-desc', label: 'Price: high to low' },
    { value: 'duration-asc', label: 'Duration: shortest first' },
    { value: 'departure-asc', label: 'Departure: earliest first' },
  ] as const;
  readonly airports = signal<Airport[]>([]);
  readonly offers = signal<FlightOffer[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal('');
  readonly hasSearched = signal(false);
  readonly selectedSort = signal<(typeof this.sortOptions)[number]['value']>('price-asc');
  readonly minDate = computed(() => new Date().toISOString().split('T')[0]);
  readonly sortedOffers = computed(() => this.sortOffers(this.offers(), this.selectedSort()));
  readonly cabinClasses = ['Economy', 'Business', 'First Class'];

  readonly searchForm = this.fb.nonNullable.group({
    originCode: ['', Validators.required],
    destinationCode: ['', Validators.required],
    departureDate: ['', Validators.required],
    passengers: [1, [Validators.required, Validators.min(1), Validators.max(9)]],
    cabinClass: ['Economy', Validators.required],
  });

  ngOnInit(): void {
    this.api.getAirports().subscribe({
      next: airports => this.airports.set(airports),
      error: err => this.errorMessage.set(this.readApiError(err, 'Could not load airports. Please refresh the page.')),
    });
  }

  onSubmit(): void {
    this.searchForm.markAllAsTouched();
    this.errorMessage.set('');
    this.hasSearched.set(true);

    const { originCode, destinationCode } = this.searchForm.getRawValue();
    if (this.searchForm.invalid) {
      return;
    }

    if (originCode === destinationCode) {
      this.errorMessage.set('Origin and destination cannot be the same.');
      return;
    }

    const request = this.buildRequest();
    this.isLoading.set(true);

    // Hold the spinner for at least MIN_LOADING_MS so the loading state is
    // perceivable even when the mock returns instantly. The API call and the
    // timer run concurrently; whichever finishes last unblocks the UI.
    forkJoin({
      response: this.api.searchFlights(request),
      _delay: timer(MIN_LOADING_MS),
    }).pipe(
      map(({ response }) => response),
      finalize(() => this.isLoading.set(false)),
    ).subscribe({
      next: response => this.offers.set(response.results),
      error: err => this.errorMessage.set(this.readApiError(err, 'Search failed. Please try again.')),
    });
  }

  durationLabel(minutes: number): string {
    const hours = Math.floor(minutes / 60);
    const remainingMinutes = minutes % 60;
    return `${hours}h ${remainingMinutes}m`;
  }

  onSortChange(sort: (typeof this.sortOptions)[number]['value']): void {
    this.selectedSort.set(sort);
  }

  goToBooking(offer: FlightOffer): void {
    this.router.navigate(['/booking'], { state: { flight: offer } });
  }

  private buildRequest(): SearchRequest {
    const value = this.searchForm.getRawValue();
    return {
      ...value,
      originCode: value.originCode.toUpperCase(),
      destinationCode: value.destinationCode.toUpperCase(),
    };
  }

  private readApiError(error: unknown, fallback: string): string {
    if (error instanceof Error && error.message) {
      return error.message;
    }

    return fallback;
  }

  private sortOffers(
    offers: FlightOffer[],
    sort: (typeof this.sortOptions)[number]['value'],
  ): FlightOffer[] {
    return offers
      .map((offer, index) => ({ offer, index }))
      .sort((a, b) => {
        let result = 0;

        switch (sort) {
          case 'price-asc':
            result = a.offer.totalPrice - b.offer.totalPrice;
            break;
          case 'price-desc':
            result = b.offer.totalPrice - a.offer.totalPrice;
            break;
          case 'duration-asc':
            result = a.offer.durationMinutes - b.offer.durationMinutes;
            break;
          case 'departure-asc':
            result = new Date(a.offer.departureTime).getTime() - new Date(b.offer.departureTime).getTime();
            break;
        }

        if (result !== 0) {
          return result;
        }

        const tieByFlightNumber = a.offer.flightNumber.localeCompare(b.offer.flightNumber);
        if (tieByFlightNumber !== 0) {
          return tieByFlightNumber;
        }

        return a.index - b.index;
      })
      .map(item => item.offer);
  }
}
