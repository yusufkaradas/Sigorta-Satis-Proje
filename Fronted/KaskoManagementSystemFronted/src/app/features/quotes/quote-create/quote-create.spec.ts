import { ComponentFixture, TestBed } from '@angular/core/testing';
import { QuoteCreate } from './quote-create';

describe('QuoteCreate', () => {
  let component: QuoteCreate;
  let fixture: ComponentFixture<QuoteCreate>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [QuoteCreate],
    }).compileComponents();

    fixture = TestBed.createComponent(QuoteCreate);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
