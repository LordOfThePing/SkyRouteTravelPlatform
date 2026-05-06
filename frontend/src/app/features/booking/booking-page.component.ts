import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { BookingRequest, PassengerForm } from '../../core/models/booking.model';
import { Airport } from '../../core/models/airport.model';
import { FlightOffer } from '../../core/models/flight.model';
import { ApiService } from '../../core/services/api.service';

type BookingState = {
  flight?: FlightOffer;
};

@Component({
  selector: 'app-booking-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    @if (!flight()) {
      <div class="card space-y-3">
        <h1 class="text-2xl font-bold text-gray-900">Book Flight</h1>
        <p class="text-sm text-gray-600">No selected flight found. Please search and choose a flight first.</p>
        <a routerLink="/search" class="btn-primary w-fit">Go to search</a>
      </div>
    } @else {
      <section class="space-y-6">
        <div class="card">
          <h1 class="text-2xl font-bold text-gray-900 mb-2">Book Flight</h1>
          <p class="text-sm text-gray-600">
            Complete passenger details to confirm this booking.
          </p>
        </div>

        <div class="card">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">Flight summary</h2>
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
              <dt class="font-medium text-gray-500">Price per passenger</dt>
              <dd>{{ flight()!.pricePerPassenger | number:'1.2-2' }} {{ flight()!.currency }}</dd>
            </div>
            <div>
              <dt class="font-medium text-gray-500">Total</dt>
              <dd class="font-semibold text-gray-900">{{ flight()!.totalPrice | number:'1.2-2' }} {{ flight()!.currency }}</dd>
            </div>
          </dl>
        </div>

        <div class="card">
          <h2 class="text-lg font-semibold text-gray-900 mb-2">Passengers</h2>
          <p class="text-sm text-gray-600">
            Document type: <span class="font-semibold">{{ documentLabel() }}</span>
          </p>

          <form class="mt-4 space-y-6" [formGroup]="bookingForm" (ngSubmit)="onSubmit()">
            <div formArrayName="passengers" class="space-y-4">
              @for (passengerGroup of passengers.controls; track $index) {
                <fieldset class="rounded-xl border border-gray-200 p-4" [formGroupName]="$index">
                  <legend class="px-1 text-sm font-semibold text-gray-700">
                    Passenger {{ $index + 1 }} of {{ passengers.length }}
                  </legend>

                  <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
                    <div>
                      <label class="label" [for]="'fullName-' + $index">Full name</label>
                      <input [id]="'fullName-' + $index" class="input-field" type="text" formControlName="fullName" />
                      @if (isFieldInvalid($index, 'fullName')) {
                        <p class="error-text">Full name is required.</p>
                      }
                    </div>

                    <div>
                      <label class="label" [for]="'email-' + $index">Email</label>
                      <input [id]="'email-' + $index" class="input-field" type="email" formControlName="email" />
                      @if (isFieldInvalid($index, 'email')) {
                        <p class="error-text">Enter a valid email address.</p>
                      }
                    </div>

                    <div class="md:col-span-2">
                      <label class="label" [for]="'documentNumber-' + $index">{{ documentLabel() }}</label>
                      <input
                        [id]="'documentNumber-' + $index"
                        class="input-field"
                        type="text"
                        formControlName="documentNumber"
                        [placeholder]="documentPlaceholder()"
                      />
                      @if (isFieldInvalid($index, 'documentNumber')) {
                        <p class="error-text">{{ documentErrorMessage() }}</p>
                      }
                    </div>
                  </div>
                </fieldset>
              }
            </div>

            <div class="flex flex-wrap items-center gap-3">
              <button type="submit" class="btn-primary" [disabled]="bookingForm.invalid || isSubmitting()">
                {{ isSubmitting() ? 'Confirming...' : 'Confirm booking' }}
              </button>
              <a routerLink="/search" class="btn-secondary">Back to search</a>
              @if (errorMessage()) {
                <p class="text-sm text-red-600">{{ errorMessage() }}</p>
              }
            </div>
          </form>
        </div>
      </section>
    }
  `,
})
export class BookingPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  readonly flight = signal<FlightOffer | null>((history.state as BookingState | undefined)?.flight ?? null);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal('');
  readonly airports = signal<Airport[]>([]);
  readonly isInternational = computed(() => {
    const selectedFlight = this.flight();
    if (!selectedFlight) {
      return false;
    }

    const originCountry = this.airports().find(airport => airport.code === selectedFlight.originCode)?.country;
    const destinationCountry = this.airports().find(airport => airport.code === selectedFlight.destinationCode)?.country;
    if (!originCountry || !destinationCountry) {
      return selectedFlight.originCode.slice(0, 1) !== selectedFlight.destinationCode.slice(0, 1);
    }

    return originCountry !== destinationCountry;
  });
  readonly documentLabel = computed(() => this.isInternational() ? 'Passport Number' : 'National ID');
  readonly documentPlaceholder = computed(() => this.isInternational() ? 'e.g. X1234567' : 'e.g. 12345678A');

  readonly bookingForm = this.fb.nonNullable.group({
    passengers: this.fb.array(this.buildPassengerControls()),
  });

  constructor() {
    this.ensureFlightExists();
    this.api.getAirports().subscribe({
      next: airports => {
        this.airports.set(airports);
        this.updateDocumentValidators();
      },
      error: () => this.updateDocumentValidators(),
    });
  }

  get passengers(): FormArray {
    return this.bookingForm.controls.passengers;
  }

  isFieldInvalid(index: number, field: 'fullName' | 'email' | 'documentNumber'): boolean {
    const control = this.passengers.at(index)?.get(field);
    return !!control && control.invalid && (control.touched || control.dirty);
  }

  documentErrorMessage(): string {
    return this.isInternational()
      ? 'Passport must be 6-12 uppercase letters or numbers.'
      : 'National ID must be 6-14 uppercase letters or numbers.';
  }

  onSubmit(): void {
    this.bookingForm.markAllAsTouched();
    this.errorMessage.set('');

    const selectedFlight = this.flight();
    if (!selectedFlight) {
      this.router.navigate(['/search']);
      return;
    }

    if (this.bookingForm.invalid) {
      return;
    }

    const payload: BookingRequest = {
      flightId: selectedFlight.id,
      originCode: selectedFlight.originCode,
      destinationCode: selectedFlight.destinationCode,
      cabinClass: selectedFlight.cabinClass,
      totalPrice: selectedFlight.totalPrice,
      passengers: this.passengers.getRawValue().map(passenger => this.toPassenger(passenger)),
    };

    this.isSubmitting.set(true);
    this.api.createBooking(payload).pipe(
      finalize(() => this.isSubmitting.set(false)),
    ).subscribe({
      next: response => {
        this.router.navigate(['/confirmation'], {
          state: {
            bookingReference: response.bookingReference,
            createdAt: response.createdAt,
            flight: selectedFlight,
            passengers: payload.passengers,
          },
        });
      },
      error: err => this.errorMessage.set(this.readApiError(err, 'Booking failed. Please check passenger data and try again.')),
    });
  }

  private ensureFlightExists(): void {
    if (!this.flight()) {
      this.router.navigate(['/search']);
    }
  }

  private buildPassengerControls() {
    const passengerCount = this.flight()?.passengers ?? 1;
    return Array.from({ length: passengerCount }, () =>
      this.fb.nonNullable.group({
        fullName: ['', [Validators.required, Validators.maxLength(100)]],
        email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
        documentNumber: ['', Validators.required],
      }),
    );
  }

  private updateDocumentValidators(): void {
    const pattern = this.isInternational()
      ? /^[A-Z0-9]{6,12}$/
      : /^[A-Z0-9]{6,14}$/;

    this.passengers.controls.forEach(group => {
      const documentControl = group.get('documentNumber');
      if (!documentControl) {
        return;
      }

      documentControl.setValidators([Validators.required, Validators.pattern(pattern), Validators.maxLength(32)]);
      documentControl.updateValueAndValidity({ emitEvent: false });
    });
  }

  private toPassenger(passenger: { fullName: string; email: string; documentNumber: string }): PassengerForm {
    return {
      fullName: passenger.fullName.trim(),
      email: passenger.email.trim(),
      documentType: this.isInternational() ? 'Passport' : 'NationalId',
      documentNumber: passenger.documentNumber.trim().toUpperCase(),
    };
  }

  private readApiError(error: unknown, fallback: string): string {
    if (error instanceof Error && error.message) {
      return error.message;
    }

    return fallback;
  }
}
