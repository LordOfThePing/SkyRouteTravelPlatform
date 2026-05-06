import { CommonModule, DatePipe } from '@angular/common';
import { Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PassengerForm } from '../../core/models/booking.model';
import { FlightOffer } from '../../core/models/flight.model';

type ConfirmationState = {
  bookingReference?: string;
  createdAt?: string;
  flight?: FlightOffer;
  passengers?: PassengerForm[];
};

@Component({
  selector: 'app-confirmation-page',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe],
  template: `
    @if (!bookingReference()) {
      <div class="card space-y-3">
        <h1 class="text-2xl font-bold text-gray-900">Booking confirmation</h1>
        <p class="text-sm text-gray-600">No confirmation data found. Start a new booking from search.</p>
        <a routerLink="/search" class="btn-primary w-fit">Go to search</a>
      </div>
    } @else {
      <section class="space-y-6">
        <div class="card">
          <h1 class="text-2xl font-bold text-gray-900 mb-2">Booking Confirmed</h1>
          <p class="text-sm text-gray-600">Your booking reference code is:</p>
          <p class="mt-3 text-3xl font-extrabold tracking-wider text-brand-700">{{ bookingReference() }}</p>
          @if (createdAt()) {
            <p class="mt-2 text-xs text-gray-500">Created at {{ createdAt() | date:'medium' }}</p>
          }
        </div>

        @if (flight()) {
          <div class="card">
            <h2 class="text-lg font-semibold text-gray-900 mb-4">Flight details</h2>
            <dl class="grid grid-cols-1 gap-3 text-sm text-gray-700 sm:grid-cols-2">
              <div>
                <dt class="font-medium text-gray-500">Provider</dt>
                <dd>{{ flight()!.provider }}</dd>
              </div>
              <div>
                <dt class="font-medium text-gray-500">Flight</dt>
                <dd>{{ flight()!.flightNumber }}</dd>
              </div>
              <div>
                <dt class="font-medium text-gray-500">Route</dt>
                <dd>{{ flight()!.originCode }} → {{ flight()!.destinationCode }}</dd>
              </div>
              <div>
                <dt class="font-medium text-gray-500">Cabin</dt>
                <dd>{{ flight()!.cabinClass }}</dd>
              </div>
              <div>
                <dt class="font-medium text-gray-500">Passengers</dt>
                <dd>{{ passengers().length }}</dd>
              </div>
              <div>
                <dt class="font-medium text-gray-500">Total paid</dt>
                <dd class="font-semibold text-gray-900">{{ flight()!.totalPrice | number:'1.2-2' }} {{ flight()!.currency }}</dd>
              </div>
            </dl>
          </div>
        }

        <div class="card">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">Passenger details</h2>
          <ul class="space-y-3">
            @for (passenger of passengers(); track $index) {
              <li class="rounded-lg border border-gray-200 p-3 text-sm text-gray-700">
                <p class="font-semibold text-gray-900">Passenger {{ $index + 1 }}: {{ passenger.fullName }}</p>
                <p>Email: {{ passenger.email }}</p>
                <p>{{ passenger.documentType === 'Passport' ? 'Passport Number' : 'National ID' }}: {{ passenger.documentNumber }}</p>
              </li>
            }
          </ul>
        </div>

        <div class="flex flex-wrap gap-3">
          <a routerLink="/search" class="btn-primary">Search new flight</a>
          <a routerLink="/booking" class="btn-secondary">Back to booking</a>
        </div>
      </section>
    }
  `,
})
export class ConfirmationPageComponent {
  private readonly state = history.state as ConfirmationState | undefined;

  readonly bookingReference = signal(this.state?.bookingReference ?? '');
  readonly createdAt = signal(this.state?.createdAt ?? '');
  readonly flight = signal<FlightOffer | null>(this.state?.flight ?? null);
  readonly passengers = signal<PassengerForm[]>(this.state?.passengers ?? []);
}
